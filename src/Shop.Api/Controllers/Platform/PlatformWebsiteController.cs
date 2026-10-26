using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
namespace Shop.Api.Controllers.Platform;
[ApiController,Route("api/platform/site")]
public sealed class PlatformWebsiteController:ControllerBase
{
    private readonly PlatformContext _db;
    public PlatformWebsiteController(PlatformContext db)=>_db=db;
    [HttpGet]
    public async Task<IActionResult> Read()
    {
        Response.Headers["Cache-Control"]="no-cache";
        await _db.Database.OpenConnectionAsync();
        try{using var cmd=_db.Database.GetDbConnection().CreateCommand();cmd.CommandText="SELECT Document,Version FROM PlatformWebsite WHERE Id=1";
            using var reader=await cmd.ExecuteReaderAsync();var doc=await reader.ReadAsync()?JsonSerializer.Deserialize<Website>(reader.GetString(0))!:new Website();
            if(reader.HasRows)doc.Version=reader.GetInt32(1);return Ok(ApiResponse<object>.Ok(doc));}
        finally{await _db.Database.CloseConnectionAsync();}
    }
    [HttpPut,Authorize(Roles="PlatformAdmin")]
    public async Task<IActionResult> Save(Website doc)
    {
        int version=doc.Version;doc.Version++;
        int changed=version==0?await _db.Database.ExecuteSqlRawAsync("INSERT INTO PlatformWebsite(Id,Version,Document) VALUES(1,{0},{1}) ON CONFLICT(Id) DO NOTHING",doc.Version,JsonSerializer.Serialize(doc)):
            await _db.Database.ExecuteSqlRawAsync("UPDATE PlatformWebsite SET Version={0},Document={1} WHERE Id=1 AND Version={2}",doc.Version,JsonSerializer.Serialize(doc),version);
        return changed==1?Ok(ApiResponse<object>.Ok(doc)):Conflict(ApiResponse<object>.Fail("Website content changed. Refresh before saving.",409));
    }
    public sealed class Website
    {
        [Range(0,int.MaxValue)]public int Version{get;set;}
        [Required,MaxLength(100)]public string BrandEn{get;set;}="Matjark";
        [Required,MaxLength(100)]public string BrandAr{get;set;}="متجرك";
        [Required,MaxLength(180)]public string HeadlineEn{get;set;}="Your next chapter starts with your own store.";
        [Required,MaxLength(180)]public string HeadlineAr{get;set;}="حكاية شغلك تستاهل متجر يشبهك.";
        [Required,MaxLength(700)]public string IntroEn{get;set;}="Bring your products, your ideas and your ambition. We give you a space to sell, manage orders and grow — while shoppers discover independent stores in one place.";
        [Required,MaxLength(700)]public string IntroAr{get;set;}="هات منتجاتك وأفكارك وطموحك. إحنا بنوفر لك مساحة تبيع وتدير طلباتك وتكبر، وللعملاء مكان يكتشفوا فيه متاجر ومنتجات مختلفة.";
        [Required,MaxLength(1500)]public string AboutEn{get;set;}="We are building a home for independent businesses. Matjark brings a merchant's own store and a shared shopping destination together, with each merchant in control of their products, stock and orders.";
        [Required,MaxLength(1500)]public string AboutAr{get;set;}="إحنا بنبني مساحة لأصحاب المشاريع. متجرك بيجمع بين متجر خاص لكل تاجر ووجهة تسوق مشتركة، وكل تاجر بيتحكم في منتجاته ومخزونه وطلباته بنفسه.";
        [Required,MaxLength(1000)]public string MissionEn{get;set;}="Make selling online more accessible, and help shoppers find the people and products behind independent businesses.";
        [Required,MaxLength(1000)]public string MissionAr{get;set;}="نسهل بداية البيع أونلاين، ونساعد العملاء يوصلوا لأصحاب المشاريع والمنتجات اللي بيقدموها.";
    }
}
