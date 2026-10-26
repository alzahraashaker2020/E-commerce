using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shop.Api.Tenancy;
using Shop.BLL.Email;
using Shop.DAL.Data;

namespace Shop.Api.Security;

public sealed class EmailOutbox : IEmailOutbox
{
    private readonly ShopContext _db;
    private readonly IDataProtector _protector;
    private readonly Shop.Api.Operations.OperationJournal _journal;private readonly string _storeId;
    public EmailOutbox(ShopContext db, IDataProtectionProvider protection, StoreScope store,Shop.Api.Operations.OperationJournal journal)
    { _db=db; _protector=protection.CreateProtector("StoreEmailOutbox",store.StoreId);_journal=journal;_storeId=store.StoreId; }

    public async Task EnqueueAsync(string to,string subject,string body,string? fileName=null,DateTime? expiresAtUtc=null)
    {
        if(string.IsNullOrWhiteSpace(to))return;
        var now=DateTime.UtcNow.ToString("O");
        await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"EmailOutbox\" (\"Id\",\"Recipient\",\"Subject\",\"Payload\",\"FileName\",\"State\",\"Attempts\",\"CreatedAtUtc\",\"NextAttemptUtc\",\"ExpiresAtUtc\",\"Lease\",\"LeaseUntilUtc\",\"LastError\",\"DeliveredAtUtc\") VALUES ({0},{1},{2},{3},{4},'Pending',0,{5},{5},{6},'','','','')",
            Guid.NewGuid().ToString("N"),to,subject,_protector.Protect(body),fileName??"",now,(expiresAtUtc??DateTime.UtcNow.AddDays(2)).ToString("O"));
    }

    public async Task<List<Entry>> ReadAsync(bool dueOnly=false)
    {
        if(dueOnly)await _db.Database.ExecuteSqlRawAsync("UPDATE \"EmailOutbox\" SET \"State\"='Failed',\"Lease\"='',\"LeaseUntilUtc\"='',\"LastError\"='Delivery interrupted. Review before retrying.' WHERE \"State\"='Sending' AND \"Attempts\">=5 AND \"LeaseUntilUtc\"<={0}",DateTime.UtcNow.ToString("O"));
        if(dueOnly)await _db.Database.ExecuteSqlRawAsync("UPDATE \"EmailOutbox\" SET \"State\"='Expired',\"Payload\"='',\"Lease\"='',\"LeaseUntilUtc\"='' WHERE \"Payload\"<>'' AND \"ExpiresAtUtc\"<={0} AND \"LeaseUntilUtc\"<={0}",DateTime.UtcNow.ToString("O"));
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command=_db.Database.GetDbConnection().CreateCommand();
            command.Transaction=_db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText="SELECT \"Id\",\"Recipient\",\"Subject\",\"State\",\"Attempts\",\"CreatedAtUtc\",\"NextAttemptUtc\",\"LastError\",\"DeliveredAtUtc\" FROM \"EmailOutbox\" "+(dueOnly?"WHERE (\"State\"='Pending' OR \"State\"='Sending') AND \"NextAttemptUtc\"<=@now AND \"LeaseUntilUtc\"<=@now ":"")+"ORDER BY \"CreatedAtUtc\" "+(dueOnly?"ASC":"DESC")+" LIMIT 100";
            if(dueOnly){var p=command.CreateParameter();p.ParameterName="now";p.Value=DateTime.UtcNow.ToString("O");command.Parameters.Add(p);}
            using var reader=await command.ExecuteReaderAsync();var result=new List<Entry>();
            while(await reader.ReadAsync())result.Add(new(reader.GetString(0),reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.GetInt32(4),reader.GetString(5),reader.GetString(6),reader.GetString(7),reader.GetString(8)));
            return result;
        }
        finally{await _db.Database.CloseConnectionAsync();}
    }

    public async Task DeliverAsync(Entry entry,EmailService transport)
    {
        string now=DateTime.UtcNow.ToString("O"),lease=Guid.NewGuid().ToString("N");
        await using(var claim=await _db.Database.BeginTransactionAsync())
        {
        int claimed=await _db.Database.ExecuteSqlRawAsync("UPDATE \"EmailOutbox\" SET \"State\"='Sending',\"Lease\"={0},\"LeaseUntilUtc\"={1},\"Attempts\"=\"Attempts\"+1 WHERE \"Id\"={2} AND (\"State\"='Pending' OR \"State\"='Sending') AND \"NextAttemptUtc\"<={3} AND \"LeaseUntilUtc\"<={3} AND \"Attempts\"={4} AND \"Attempts\"<5",lease,DateTime.UtcNow.AddMinutes(2).ToString("O"),entry.Id,now,entry.Attempts);
        if(claimed!=1)return;
        await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"EmailAttempts\" VALUES ({0},{1},{2},'Started')",lease,entry.Id,now);
        await claim.CommitAsync();
        }
        string body="",file="",expiry="";
        await _db.Database.OpenConnectionAsync();
        try
        {
            using var command=_db.Database.GetDbConnection().CreateCommand();
            command.CommandText="SELECT \"Payload\",\"FileName\",\"ExpiresAtUtc\" FROM \"EmailOutbox\" WHERE \"Id\"=@id";
            var p=command.CreateParameter();p.ParameterName="id";p.Value=entry.Id;command.Parameters.Add(p);
            using var reader=await command.ExecuteReaderAsync();
            if(!await reader.ReadAsync())return;
            body=reader.GetString(0);file=reader.GetString(1);expiry=reader.GetString(2);
        }
        finally{await _db.Database.CloseConnectionAsync();}
        string outcome,state,error="";
        if(string.CompareOrdinal(expiry,now)<=0){outcome=state="Expired";}
        else
        {
            try{outcome=state=await transport.DeliverAsync(entry.Recipient,entry.Subject,_protector.Unprotect(body),file==""?null:file);}
            catch(Exception){outcome="Failed";state=entry.Attempts+1>=5?"Failed":"Pending";error="Delivery failed. Check sender settings or provider availability.";}
        }
        await using var transaction=await _db.Database.BeginTransactionAsync();
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"EmailOutbox\" SET \"State\"={0},\"LastError\"={1},\"Lease\"='',\"LeaseUntilUtc\"='',\"NextAttemptUtc\"={2},\"DeliveredAtUtc\"={3},\"Payload\"={4} WHERE \"Id\"={5} AND \"Lease\"={6}",state,error,DateTime.UtcNow.AddSeconds(Math.Min(3600,30*Math.Pow(2,entry.Attempts))).ToString("O"),state is "Sent" or "DevelopmentOnly"?DateTime.UtcNow.ToString("O"):"",state is "Sent" or "DevelopmentOnly" or "Expired"?"":body,entry.Id,lease);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"EmailAttempts\" SET \"Outcome\"={0} WHERE \"Id\"={1}",outcome,lease);
        await transaction.CommitAsync();
        if(outcome=="Failed")await _journal.WriteAsync(_storeId,"email-worker","System","EmailDeliveryFailed","JOB",500,entry.Id,"DeliveryFailure");
    }
    public sealed record Entry(string Id,string Recipient,string Subject,string State,int Attempts,string CreatedAtUtc,string NextAttemptUtc,string LastError,string DeliveredAtUtc);
}
