using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.Data;
using Shop.DAL.ViewModels;

namespace Shop.Api.Controllers;
[ApiController,Route("api/storefront")]
public sealed class StorefrontController:ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices]StorefrontContent content){Response.Headers["Cache-Control"]="no-cache";var doc=await content.PublicAsync();if(!PlanFeatures.Allows(HttpContext.RequestServices.GetRequiredService<DeploymentProfile>(),HttpContext.RequestServices.GetRequiredService<StoreScope>(),"Newsletter"))doc.NewsletterEnabled=false;return Ok(ApiResponse<object>.Ok(doc));}
    [HttpGet("products")]
    public async Task<IActionResult> Products([FromServices]ShopContext db,[FromServices]IMapper mapper,[FromServices]StorefrontContent content,[FromQuery]int page=1,[FromQuery]int pageSize=24,[FromQuery]int? categoryId=null,[FromQuery,MaxLength(120)]string? q=null,[FromQuery,MaxLength(600)]string? ids=null)
    {
        var query=db.Products.AsNoTracking().Where(p=>p.IsActive);
        if(categoryId.HasValue){var extra=(await content.ReadAsync()).Merchandising.Where(m=>m.CollectionIds.Contains(categoryId.Value)).Select(m=>m.ProductId).ToArray();var childCategories=await db.Categories.Where(c=>c.Id==categoryId||c.ParentCategoryId==categoryId).Select(c=>c.Id).ToArrayAsync();query=query.Where(p=>childCategories.Contains(p.CategoryId)||extra.Contains(p.Id));}
        if(!string.IsNullOrWhiteSpace(q)){string search=q.Trim().ToLower();var translated=(await content.ReadAsync()).Translations.Where(t=>t.Value.Contains(q.Trim(),StringComparison.OrdinalIgnoreCase)).Select(t=>t.Key).ToArray();query=query.Where(p=>p.Name.ToLower().Contains(search)||(p.Brand!=null&&p.Brand.ToLower().Contains(search))||translated.Contains(p.Name)||(p.Brand!=null&&translated.Contains(p.Brand)));}
        if(ids is not null){var pieces=ids.Split(',',StringSplitOptions.RemoveEmptyEntries);if(pieces.Length>100||pieces.Any(p=>!int.TryParse(p,out _)))return BadRequest(ApiResponse<object>.Fail("Invalid product selection.",400));var selected=pieces.Select(int.Parse).ToArray();query=query.Where(p=>selected.Contains(p.Id));}
        int total=await query.CountAsync();page=Math.Clamp(page,1,100000);pageSize=Math.Clamp(pageSize,1,100);
        var rows=await query.OrderByDescending(p=>p.Id).Skip((page-1)*pageSize).Take(pageSize).Include(p=>p.Category).Include(p=>p.Images).Include(p=>p.Variants).ThenInclude(v=>v.Size).Include(p=>p.Variants).ThenInclude(v=>v.Color).ToListAsync();
        foreach(var row in rows)row.Variants=row.Variants.Where(v=>v.IsActive).ToList();
        return Ok(ApiResponse<object>.Ok(new{Items=mapper.Map<List<ProductDetailVM>>(rows),TotalCount=total,Page=page,PageSize=pageSize}));
    }
    [HttpPost("subscribe"),EnableRateLimiting("AccountRegistration")]
    public async Task<IActionResult> Subscribe(SubscribeBody request,[FromServices]StorefrontContent content,[FromServices]ShopContext db)
    {
        if(!request.Consent||!(await content.ReadAsync()).NewsletterEnabled)return BadRequest(ApiResponse<object>.Fail("Newsletter signup is unavailable or consent is missing.",400));
        await db.Database.ExecuteSqlRawAsync("INSERT INTO \"StorefrontSubscribers\" VALUES ({0},{1}) ON CONFLICT (\"Email\") DO NOTHING",request.Email.Trim().ToLowerInvariant(),DateTime.UtcNow.ToString("O"));
        return Ok(ApiResponse<object>.Ok(new{Message="Thanks! Your signup has been recorded."}));
    }
    public sealed class SubscribeBody{[Required,EmailAddress,MaxLength(200)]public string Email{get;set;}="";public bool Consent{get;set;}}
}
