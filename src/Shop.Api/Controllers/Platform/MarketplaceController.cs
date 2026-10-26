using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
namespace Shop.Api.Controllers.Platform;

// Deliberately projects public catalog fields only, never reuses merchant/private controllers.
[ApiController,Route("api/platform/marketplace")]
public sealed class MarketplaceController:ControllerBase
{
    [HttpGet,EnableRateLimiting("Marketplace")]
    public async Task<IActionResult> Browse([FromServices]PlatformContext registry,[FromServices]IConfiguration config,
        [FromServices]ILogger<MarketplaceController> logger,[FromQuery]int page=1,[FromQuery]int pageSize=24,
        [FromQuery,MaxLength(120)]string? q=null,[FromQuery,MaxLength(80)]string? storeId=null)
    {
        page=Math.Clamp(page,1,100000);pageSize=Math.Clamp(pageSize,1,48);
        var stores=await registry.Stores.AsNoTracking().Where(s=>s.State=="Active").OrderBy(s=>s.Id).ToListAsync();
        var subscriptions=await registry.Subscriptions.AsNoTracking().ToDictionaryAsync(s=>s.StoreId);
        stores=stores.Where(s=>!s.SubscriptionRequired || (subscriptions.TryGetValue(s.Id,out var sub)&&SubscriptionAccess.IsCurrent(sub)&&PlanFeatures.Read(sub.FeaturesJson).Contains("Marketplace"))).ToList();
        var rows=new List<object>();var visibleStores=new List<object>();int total=0,skip=(page-1)*pageSize,remaining=pageSize,unavailable=0;
        foreach(var store in stores)
        {
            if(!string.IsNullOrEmpty(storeId)&&storeId!=store.Id)continue;
            try
            {
                var options=new DbContextOptionsBuilder<ShopContext>();StoreConnections.Configure(options,config,store.ConnectionKey);
                await using var db=new ShopContext(options.Options);var scope=new StoreScope {Store=store};
                var content=await new StorefrontContent(db,scope).ReadAsync();
                if(!content.IncludeInMarketplace)continue;
                var query=db.Products.AsNoTracking().Where(p=>p.IsActive);
                if(!string.IsNullOrWhiteSpace(q))
                {
                    var search=q.Trim().ToLower();var translated=content.Translations.Where(t=>t.Value.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase)).Select(t=>t.Key).ToArray();
                    query=query.Where(p=>p.Name.ToLower().Contains(search)||(p.Brand!=null&&p.Brand.ToLower().Contains(search))||translated.Contains(p.Name));
                }
                int count=await query.CountAsync();total+=count;visibleStores.Add(new{store.Id,Name=content.BrandName,store.Host});
                if(skip>=count){skip-=count;continue;}if(remaining==0)continue;
                var products=await query.OrderByDescending(p=>p.Id).Skip(skip).Take(remaining).Include(p=>p.Images).Include(p=>p.Variants).ToListAsync();skip=0;
                var currency=await db.StoreSettings.Where(s=>s.Id==1).Select(s=>s.CurrencyCode).SingleOrDefaultAsync()??"EGP";
                foreach(var p in products)
                {
                    var variants=p.Variants.Where(v=>v.IsActive).ToArray();
                    var image=p.Images.OrderBy(i=>i.SortOrder).Select(i=>i.Url).FirstOrDefault()??variants.Select(v=>v.ImageUrl).FirstOrDefault(i=>!string.IsNullOrEmpty(i));
                    rows.Add(new{StoreId=store.Id,StoreName=content.BrandName,store.Host,ProductId=p.Id,p.Name,
                        NameAr=content.Translations.GetValueOrDefault(p.Name,p.Name),ImageUrl=image,
                        Price=p.HasVariants&&variants.Length>0?variants.Min(v=>v.Price):p.BasePrice,Currency=currency,
                        Available=p.HasVariants?variants.Any(v=>v.StockQuantity>0):p.StockQuantity>0});
                }
                remaining-=products.Count;
            }
            catch(Exception ex) when(ex is not OperationCanceledException)
            {unavailable++;logger.LogWarning("Public marketplace temporarily omitted store {StoreId} ({ErrorType})",store.Id,ex.GetType().Name);}
        }
        Response.Headers["Cache-Control"]="no-store";
        return Ok(ApiResponse<object>.Ok(new{Items=rows,Stores=visibleStores,TotalCount=total,Page=page,PageSize=pageSize,Partial=unavailable>0}));
    }
}
