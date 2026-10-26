using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Shop.DAL.Data;

namespace Shop.Api.Tenancy;

public sealed class StorefrontContent
{
    private readonly ShopContext _db;private readonly StoreScope _store;
    private readonly JsonSerializerOptions _json=new(JsonSerializerDefaults.Web);
    public StorefrontContent(ShopContext db,StoreScope store){_db=db;_store=store;}
    public async Task<Document> ReadAsync()
    {
        await _db.Database.OpenConnectionAsync();
        try{using var cmd=_db.Database.GetDbConnection().CreateCommand();cmd.CommandText="SELECT \"Document\",\"Version\" FROM \"StorefrontContent\" WHERE \"Id\"=1";using var reader=await cmd.ExecuteReaderAsync();if(!await reader.ReadAsync())return new(){BrandName=_store.Store!.Name};var value=JsonSerializer.Deserialize<Document>(reader.GetString(0),_json)!;value.Version=reader.GetInt32(1);return value;}
        finally{await _db.Database.CloseConnectionAsync();}
    }
    public async Task<bool> SaveAsync(Document document)
    {
        if(document.BusinessMode is not ("Commerce" or "Catalog"))throw new InvalidOperationException("Select Commerce or Catalog for the store activity.");
        if(document.Theme is not ("classic" or "kids"))throw new InvalidOperationException("Select a supported storefront style.");
        if(!Regex.IsMatch(document.PrimaryColor,"^#[0-9a-fA-F]{6}$"))throw new InvalidOperationException("Choose a valid brand color.");
        if(document.Merchandising.GroupBy(m=>m.ProductId).Any(g=>g.Count()>1))throw new InvalidOperationException("Use one merchandising entry per product.");
        if(document.Navigation.Count>10||document.Promotions.Count>4||document.Categories.Count>12||document.Ages.Count>8||document.Banners.Count>2||document.Sections.Count>6||document.Benefits.Count>6||document.Merchandising.Count>100)throw new InvalidOperationException("Too many storefront items.");
        var images=document.Categories.Concat(document.Ages).Select(c=>c.ImageUrl).Concat(document.Banners.Select(b=>b.ImageUrl));
        foreach(var image in images)if(image==""||!image.StartsWith("/uploads/stores/"+_store.StoreId+"/",StringComparison.Ordinal)||image.Contains("..")||image.Contains('%')||image.Contains('\\')||image.Contains('?')||image.Contains('#')||!Regex.IsMatch(image,@"\.(webp|png|jpg|jpeg)$",RegexOptions.IgnoreCase))throw new InvalidOperationException("Use images uploaded to this store for every photo card and banner.");
        var categoryIds=document.Navigation.Concat(document.Promotions).Select(n=>n.CategoryId).Concat(document.Categories.Select(n=>n.CategoryId)).Concat(document.Ages.Select(n=>n.CategoryId)).Concat(document.Banners.Select(n=>n.CategoryId)).Concat(document.Sections.Select(n=>n.CategoryId)).Where(id=>id.HasValue).Select(id=>id!.Value).Concat(document.Merchandising.SelectMany(m=>m.CollectionIds)).Distinct().ToList();
        if(await _db.Categories.CountAsync(c=>categoryIds.Contains(c.Id))!=categoryIds.Count)throw new InvalidOperationException("Choose categories belonging to this store.");
        var productIds=document.Sections.SelectMany(s=>s.ProductIds).Concat(document.Merchandising.Select(m=>m.ProductId)).Distinct().ToList();
        if(productIds.Count>100||document.Sections.Any(s=>s.ProductIds.Count>16)||await _db.Products.CountAsync(p=>productIds.Contains(p.Id))!=productIds.Count)throw new InvalidOperationException("Choose products belonging to this store.");
        foreach(var banner in document.Banners)if(!Regex.IsMatch(banner.BackgroundColor,"^#[0-9a-fA-F]{6}$")||!Regex.IsMatch(banner.TextColor,"^#[0-9a-fA-F]{6}$"))throw new InvalidOperationException("Choose valid banner colors.");
        if(document.SupportEmail!=""&&!new EmailAddressAttribute().IsValid(document.SupportEmail))throw new InvalidOperationException("Enter a valid support email.");
        if(document.Translations is null || document.Translations.Count>2000 || document.Translations.Any(p=>string.IsNullOrWhiteSpace(p.Key)||p.Key.Length>64000||p.Value is null||p.Value.Length>64000)||document.Translations.Sum(p=>p.Key.Length+p.Value.Length)>2000000)
            throw new InvalidOperationException("Translation content is too large or invalid.");
        document.Version++;
        int changed=document.Version==1
            ?await _db.Database.ExecuteSqlRawAsync("INSERT INTO \"StorefrontContent\" VALUES (1,{0},{1}) ON CONFLICT (\"Id\") DO NOTHING",document.Version,JsonSerializer.Serialize(document,_json))
            :await _db.Database.ExecuteSqlRawAsync("UPDATE \"StorefrontContent\" SET \"Version\"={0},\"Document\"={1} WHERE \"Id\"=1 AND \"Version\"={2}",document.Version,JsonSerializer.Serialize(document,_json),document.Version-1);
        return changed==1;
    }
    public sealed record TranslationSource(string Source,string Context);
    public async Task<List<TranslationSource>> TranslationSourcesAsync(bool publicOnly=false)
    {
        var rows=new List<TranslationSource>();
        void Add(string? text,string context){if(!string.IsNullOrWhiteSpace(text)&&text.Length<=64000)rows.Add(new(text,context));}
        var doc=await ReadAsync();
        foreach(var text in new[]{doc.BrandName,doc.BrandSubtitle,doc.Announcement,doc.ShippingText,doc.IntroEyebrow,doc.IntroTitle,doc.AgesTitle,doc.FooterText,doc.NewsletterTitle,doc.NewsletterSubtitle})Add(text,"Home page");
        foreach(var link in doc.Navigation.Concat(doc.Promotions).Concat(doc.Categories).Concat(doc.Ages)){Add(link.Label,"Collections");Add(link.Subtitle,"Collections");}
        foreach(var b in doc.Banners){Add(b.Title,"Campaign banners");Add(b.Subtitle,"Campaign banners");Add(b.Eyebrow,"Campaign banners");Add(b.ButtonLabel,"Campaign banners");}
        foreach(var b in doc.Sections){Add(b.Title,"Featured product selections");Add(b.Subtitle,"Featured product selections");Add(b.Eyebrow,"Featured product selections");}
        foreach(var b in doc.Benefits){Add(b.Title,"Service highlights");Add(b.Subtitle,"Service highlights");}
        foreach(var m in doc.Merchandising)Add(m.Badge,"Product labels");
        var products=await _db.Products.AsNoTracking().Where(p=>!publicOnly||p.IsActive).Select(p=>new{p.Name,p.Brand,p.DescriptionHtml}).ToListAsync();
        foreach(var p in products){Add(p.Name,"Product name");Add(p.Brand,"Brand");Add(p.DescriptionHtml,"Product description");}
        foreach(var text in await _db.Categories.AsNoTracking().Select(p=>p.Name).ToListAsync())Add(text,"Categories");
        foreach(var text in await _db.Sizes.AsNoTracking().Where(p=>!publicOnly||p.IsActive).Select(p=>p.Name).ToListAsync())Add(text,"Sizes");
        foreach(var text in await _db.Colors.AsNoTracking().Where(p=>!publicOnly||p.IsActive).Select(p=>p.Name).ToListAsync())Add(text,"Colors");
        foreach(var text in await _db.OrderStatusDefinitions.AsNoTracking().Where(p=>!publicOnly||p.IsActive).Select(p=>p.Name).ToListAsync())Add(text,"Order statuses");
        foreach(var text in await _db.Governorates.AsNoTracking().Where(p=>!publicOnly||p.IsActive).Select(p=>p.Name).ToListAsync())Add(text,"Governorate");
        foreach(var text in await _db.DeliveryAreas.AsNoTracking().Where(p=>!publicOnly||p.IsActive).Select(p=>p.Name).ToListAsync())Add(text,"Delivery area");
        return rows.DistinctBy(r=>r.Source,StringComparer.Ordinal).ToList();
    }
    public async Task<Document> PublicAsync()
    {
        var doc=await ReadAsync();
        if(doc.Translations.Count>0){var allowed=(await TranslationSourcesAsync(true)).Select(r=>r.Source).ToHashSet(StringComparer.Ordinal);doc.Translations=doc.Translations.Where(p=>allowed.Contains(p.Key)).ToDictionary(p=>p.Key,p=>p.Value);}
        return doc;
    }
    public sealed class Document
    {
        public bool IncludeInMarketplace{get;set;}=true;
        [Range(0,int.MaxValue)]public int Version{get;set;}
        [Required,MaxLength(20)]public string BusinessMode{get;set;}="Commerce";
        [Required,MaxLength(20)]public string Theme{get;set;}="classic";
        [Required,MaxLength(80)]public string BrandName{get;set;}="Shop";
        [MaxLength(100)]public string BrandSubtitle{get;set;}="";
        [MaxLength(180)]public string Announcement{get;set;}="";
        [MaxLength(160)]public string ShippingText{get;set;}="";
        [Required,MaxLength(7)]public string PrimaryColor{get;set;}="#426fb0";
        [MaxLength(120)]public string IntroEyebrow{get;set;}="Discover";
        [MaxLength(180)]public string IntroTitle{get;set;}="Everyday favourites for little adventures";
        [MaxLength(180)]public string AgesTitle{get;set;}="Shop by age";
        [MaxLength(200)]public string FooterText{get;set;}="";
        [MaxLength(160)]public string SupportEmail{get;set;}="";
        [MaxLength(60)]public string SupportPhone{get;set;}="";
        public Dictionary<string,string> Translations{get;set;}=new();
        public bool NewsletterEnabled{get;set;}
        [MaxLength(120)]public string NewsletterTitle{get;set;}="Join the little club.";
        [MaxLength(180)]public string NewsletterSubtitle{get;set;}="New collections, little favourites and lovely offers.";
        public List<Link> Navigation{get;set;}=new();public List<Link> Promotions{get;set;}=new();public List<Tile> Categories{get;set;}=new();public List<Tile> Ages{get;set;}=new();public List<Banner> Banners{get;set;}=new();public List<ProductSection> Sections{get;set;}=new();public List<Benefit> Benefits{get;set;}=new();public List<ProductMerchandising> Merchandising{get;set;}=new();
    }
    public class Link{[Required,MaxLength(80)]public string Label{get;set;}="";[MaxLength(120)]public string Subtitle{get;set;}="";public int? CategoryId{get;set;}}
    public sealed class Tile:Link{[MaxLength(500)]public string ImageUrl{get;set;}="";}
    public sealed class Banner
    {
        [Required,MaxLength(120)]public string Title{get;set;}="";[MaxLength(180)]public string Subtitle{get;set;}="";[MaxLength(60)]public string Eyebrow{get;set;}="";[MaxLength(60)]public string ButtonLabel{get;set;}="Shop now";[MaxLength(500)]public string ImageUrl{get;set;}="";public int? CategoryId{get;set;}public string BackgroundColor{get;set;}="#e8edf2";public string TextColor{get;set;}="#172c47";
    }
    public sealed class ProductSection{[Required,MaxLength(120)]public string Title{get;set;}="";[MaxLength(180)]public string Subtitle{get;set;}="";[MaxLength(60)]public string Eyebrow{get;set;}="";public int? CategoryId{get;set;}public List<int> ProductIds{get;set;}=new();}
    public sealed class Benefit{[Required,MaxLength(100)]public string Title{get;set;}="";[MaxLength(160)]public string Subtitle{get;set;}="";}
    public sealed class ProductMerchandising{public int ProductId{get;set;}[Range(0,10000000)]public decimal? CompareAtPrice{get;set;}[MaxLength(40)]public string Badge{get;set;}="";public List<int> CollectionIds{get;set;}=new();}
}
