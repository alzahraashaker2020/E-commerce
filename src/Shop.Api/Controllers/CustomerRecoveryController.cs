using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Security;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;
using Shop.DAL.Data;

namespace Shop.Api.Controllers;

[ApiController,Route("api/account"),Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("AccountRecovery")]
public sealed class CustomerRecoveryController:ControllerBase
{
    private readonly ShopContext _db; private readonly CustomerEmailSecurity _security;private readonly IPasswordHasherService _passwords;
    public CustomerRecoveryController(ShopContext db,CustomerEmailSecurity security,IPasswordHasherService passwords){_db=db;_security=security;_passwords=passwords;}
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(EmailRequest request)
    {
        Response.Headers.CacheControl="no-store";
        string email=request.Email.Trim().ToLowerInvariant();
        var customer=await _db.Customers.FirstOrDefaultAsync(c=>c.Email.ToLower()==email);
        if(customer is not null&&!string.IsNullOrWhiteSpace(customer.PasswordHash))await _security.IssueAsync(customer,"ResetPassword");
        return Ok(ApiResponse<object>.Ok(new {message="If this account is eligible, a password reset message will be sent."}));
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetRequest request)=>await Result(request.Token,"ResetPassword",request.Password);
    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify(TokenRequest request)=>await Result(request.Token,"VerifyEmail",null);
    private async Task<IActionResult> Result(string token,string purpose,string? password)
    {
        Response.Headers.CacheControl="no-store";
        bool valid=await _security.ConsumeAsync(token,purpose,password,_passwords);
        return valid?Ok(ApiResponse<object>.Ok(new {message=purpose=="ResetPassword"?"Password updated. Sign in again on your devices.":"Email verified."})):
            BadRequest(ApiResponse<object>.Fail("This link is invalid, expired or already used. Request a new one.",400));
    }
    [Authorize(Roles="Customer"),HttpPost("resend-verification")]
    public async Task<IActionResult> Resend()
    {
        int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var customer=await _db.Customers.FindAsync(id);
        if(customer is not null)await _security.IssueAsync(customer,"VerifyEmail");
        return Ok(ApiResponse<object>.Ok(new {message="If verification is needed, a message will be sent."}));
    }
    [Authorize(Roles="Customer"),HttpGet("email-status")]
    public async Task<IActionResult> Status()
    {
        var customer=await _db.Customers.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        return Ok(ApiResponse<object>.Ok(new {verified=customer is not null&&await _security.IsVerified(customer)}));
    }
    public class EmailRequest{[Required,EmailAddress,MaxLength(200)]public string Email{get;set;}="";}
    public class TokenRequest{[Required,StringLength(100)]public string Token{get;set;}="";}
    public class ResetRequest:TokenRequest{[Required,StringLength(128,MinimumLength=12)]public string Password{get;set;}="";}
}
