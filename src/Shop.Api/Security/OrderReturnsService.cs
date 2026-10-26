using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.BLL.Email;

namespace Shop.Api.Security;

public sealed class OrderReturnsService
{
    private readonly ShopContext _db;private readonly IEmailOutbox _mail;
    public OrderReturnsService(ShopContext db,IEmailOutbox mail){_db=db;_mail=mail;}
    private static string Money(decimal amount)=>amount.ToString("0.00",CultureInfo.InvariantCulture);
    public async Task<List<Entry>> ReadAsync(int? orderId=null)
    {
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command=_db.Database.GetDbConnection().CreateCommand();command.Transaction=_db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText="SELECT r.\"Id\",r.\"OrderId\",r.\"OrderItemId\",r.\"Quantity\",r.\"Reason\",r.\"State\",r.\"Restocked\",r.\"MaxRefund\",r.\"RefundAmount\",r.\"RefundReference\",r.\"Notes\",r.\"Version\",r.\"CreatedAtUtc\",o.\"OrderNumber\",o.\"CurrencyCode\",i.\"ProductNameSnapshot\" FROM \"ReturnRequests\" r JOIN \"Orders\" o ON o.\"Id\"=r.\"OrderId\" JOIN \"OrderItems\" i ON i.\"Id\"=r.\"OrderItemId\" "+(orderId.HasValue?"WHERE r.\"OrderId\"=@order ":"")+"ORDER BY r.\"CreatedAtUtc\" DESC";
            if(orderId.HasValue){var p=command.CreateParameter();p.ParameterName="order";p.Value=orderId.Value;command.Parameters.Add(p);}
            using var reader=await command.ExecuteReaderAsync();var rows=new List<Entry>();
            while(await reader.ReadAsync())rows.Add(new(reader.GetString(0),reader.GetInt32(1),reader.GetInt32(2),reader.GetInt32(3),reader.GetString(4),reader.GetString(5),reader.GetInt32(6)==1,decimal.Parse(reader.GetString(7),CultureInfo.InvariantCulture),decimal.Parse(reader.GetString(8),CultureInfo.InvariantCulture),reader.GetString(9),reader.GetString(10),reader.GetInt32(11),reader.GetString(12),reader.GetString(13),reader.GetString(14),reader.GetString(15)));
            return rows;
        }
        finally{await _db.Database.CloseConnectionAsync();}
    }
    public async Task<Entry> RequestAsync(Order order,Guid key,int itemId,int quantity,string reason)
    {
        await using var transaction=await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var entries=await ReadAsync(order.Id);string id=key.ToString("N");
        var previous=entries.FirstOrDefault(e=>e.Id==id);
        if(previous is not null){if(previous.OrderItemId!=itemId||previous.Quantity!=quantity||previous.Reason!=reason)throw new InvalidOperationException("This return attempt was already used with different details.");return previous;}
        // Reload status inside the transaction, rather than trusting the access-check snapshot.
        await _db.Entry(order).ReloadAsync();
        if(order.Status!=OrderStatus.Delivered||!order.StockDeducted)throw new InvalidOperationException("Only delivered orders can be returned.");
        var line=await _db.OrderItems.SingleOrDefaultAsync(i=>i.Id==itemId&&i.OrderId==order.Id);
        if(line is null||quantity<1||quantity>line.Quantity)throw new InvalidOperationException("Select a valid order item and quantity.");
        long reserved=entries.Where(e=>e.OrderItemId==itemId&&e.State!="Rejected").Sum(e=>(long)e.Quantity);
        if(reserved+quantity>line.Quantity)throw new InvalidOperationException("This quantity already has a return request.");
        decimal refundable=Math.Max(0,order.Total-order.ShippingTotal);
        decimal max=order.Subtotal<=0?0:decimal.Floor(refundable*line.LineTotal/order.Subtotal*quantity/line.Quantity*100)/100;
        await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"ReturnRequests\" VALUES ({0},{1},{2},{3},{4},'Requested',0,{5},'0.00','','',1,{6})",id,order.Id,itemId,quantity,reason,Money(max),DateTime.UtcNow.ToString("O"));
        await Audit(id,"Requested","Customer",reason);
        await transaction.CommitAsync();return (await ReadAsync(order.Id)).Single(e=>e.Id==id);
    }
    public async Task<Entry> ChangeAsync(string id,int version,string action,string notes,bool restock,decimal refund,string reference,string actor)
    {
        await using var transaction=await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var entry=(await ReadAsync()).SingleOrDefault(e=>e.Id==id)??throw new InvalidOperationException("Return not found.");
        if(entry.Version!=version)throw new InvalidOperationException("This return changed. Refresh and try again.");
        string next=action switch
        {
            "Approve" when entry.State=="Requested"=>"Approved",
            "Reject" when entry.State is "Requested" or "Approved"=>"Rejected",
            "Receive" when entry.State=="Approved"=>"Received",
            "RecordRefund" when entry.State=="Received"=>"Completed",
            _=>throw new InvalidOperationException("This action is unavailable for the current return status.")
        };
        if(notes.Trim().Length==0)throw new InvalidOperationException("Add a reason or inspection note.");
        if(action!="Receive"&&restock)throw new InvalidOperationException("Stock can be restored only during inspection.");
        if(action=="RecordRefund"&&(refund<0||refund>entry.MaxRefund||decimal.Round(refund,2)!=refund||(refund>0&&string.IsNullOrWhiteSpace(reference))))throw new InvalidOperationException("Enter a valid refund amount and external payment reference.");
        int changed=await _db.Database.ExecuteSqlRawAsync("UPDATE \"ReturnRequests\" SET \"State\"={0},\"Notes\"={1},\"Version\"=\"Version\"+1,\"Restocked\"={2},\"RefundAmount\"={3},\"RefundReference\"={4} WHERE \"Id\"={5} AND \"Version\"={6}",next,notes,entry.Restocked||restock?1:0,action=="RecordRefund"?Money(refund):Money(entry.RefundAmount),action=="RecordRefund"?reference:entry.RefundReference,id,version);
        if(changed!=1)throw new InvalidOperationException("This return changed. Refresh and try again.");
        var order=await _db.Orders.Include(o=>o.Customer).SingleAsync(o=>o.Id==entry.OrderId);
        if(action=="Receive"&&restock)
        {
            if(!order.StockDeducted)throw new InvalidOperationException("This order did not consume stock.");
            var item=await _db.OrderItems.SingleAsync(i=>i.Id==entry.OrderItemId&&i.OrderId==entry.OrderId);
            int restored=item.VariantId.HasValue
                ?await _db.ProductVariants.Where(v=>v.Id==item.VariantId&&v.ProductId==item.ProductId&&v.StockQuantity<=int.MaxValue-entry.Quantity).ExecuteUpdateAsync(s=>s.SetProperty(v=>v.StockQuantity,v=>v.StockQuantity+entry.Quantity))
                :await _db.Products.Where(p=>p.Id==item.ProductId&&!p.HasVariants&&p.StockQuantity<=int.MaxValue-entry.Quantity).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.StockQuantity,p=>p.StockQuantity+entry.Quantity));
            if(restored!=1)throw new InvalidOperationException("The original stock item is unavailable. Inspect without restocking or restore the original item first.");
        }
        await Audit(id,action,actor,notes);
        string to=order.Customer?.Email??order.GuestEmail??"";
        await _mail.EnqueueAsync(to,$"Return update for order {order.OrderNumber}",$"Return {id}\nOrder {order.OrderNumber}\nProduct: {entry.ProductName}\nQuantity: {entry.Quantity}\nStatus: {next}\nNotes: {notes}\nRecorded refund: {(action=="RecordRefund"?Money(refund):Money(entry.RefundAmount))} {order.CurrencyCode}\nRefund records reflect a manual payment; this system has not transferred funds.");
        await transaction.CommitAsync();return (await ReadAsync(entry.OrderId)).Single(e=>e.Id==id);
    }
    private Task Audit(string id,string action,string actor,string notes)=>_db.Database.ExecuteSqlRawAsync("INSERT INTO \"ReturnEvents\" VALUES ({0},{1},{2},{3},{4},{5})",Guid.NewGuid().ToString("N"),id,action,actor,notes,DateTime.UtcNow.ToString("O"));
    public sealed record Entry(string Id,int OrderId,int OrderItemId,int Quantity,string Reason,string State,bool Restocked,decimal MaxRefund,decimal RefundAmount,string RefundReference,string Notes,int Version,string CreatedAtUtc,string OrderNumber,string CurrencyCode,string ProductName);
}
