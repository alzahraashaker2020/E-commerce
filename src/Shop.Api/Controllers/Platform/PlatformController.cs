using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;
using Shop.DAL.Data;
using Shop.DAL.Models;
using Shop.SL.TokenManager;

namespace Shop.Api.Controllers.Platform;

[ApiController, Route("api/platform")]
public sealed class PlatformController : ControllerBase
{
    private readonly Shop.Application.Provisioning.StoreProvisioningService _provisioning;
    private readonly PlatformContext _platform;
    private readonly IConfiguration _config;
    private readonly IPasswordHasherService _passwords;
    private readonly ITokenService _tokens;
    public PlatformController(PlatformContext platform, IConfiguration config, IPasswordHasherService passwords, ITokenService tokens, Shop.Application.Provisioning.StoreProvisioningService provisioning)
    { _platform = platform; _config = config; _passwords = passwords; _tokens = tokens; _provisioning = provisioning; }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _platform.Operators.SingleOrDefaultAsync(a => a.Username == request.Username);
        if (user is null || !_passwords.Verify(user.PasswordHash, request.Password))
            return Unauthorized(ApiResponse<object>.Fail("Invalid credentials", 401));
        var token = _tokens.GeneratePlatformToken(user.Id, user.Username);
        return Ok(ApiResponse<object>.Ok(new { token.Token, token.Expires, user.Username, Role = "PlatformAdmin" }));
    }

    [Authorize(Roles = "PlatformAdmin"), HttpGet("stores")]
    public async Task<IActionResult> Stores() => Ok(ApiResponse<object>.Ok(await _platform.Stores.AsNoTracking()
        .Select(s => new { s.Id, s.Name, s.Host, s.State }).ToListAsync()));

    [Authorize(Roles = "PlatformAdmin"), HttpPost("stores")]
    public async Task<IActionResult> Create(CreateStoreRequest request) => Shop.Api.Mapping.ProvisioningOutcomeMapper.Map(await _provisioning.CreateAsync(Shop.Api.Adapters.CheckoutBoundary.Copy<Shop.Application.Provisioning.ProvisioningRequest>(request), Actor));

    [Authorize(Roles = "PlatformAdmin"), HttpPost("onboarding/applications/{id}/approve")]
    public async Task<IActionResult> Approve(string id, ApproveRequest request) => Shop.Api.Mapping.ProvisioningOutcomeMapper.Map(await _provisioning.ApproveAsync(new(id, request.Version, request.Host, request.ConnectionKey, request.PaymentVerified, request.PaymentReference, Actor)));
    private string Actor => User.FindFirst("actor_id")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "platform";

    [Authorize(Roles = "PlatformAdmin"), HttpPut("stores/{id}/state")]
    public async Task<IActionResult> State(string id, StateRequest request)
    {
        var store = await _platform.Stores.FindAsync(id);
        if (store is null) return NotFound();
        if (request.State is not ("Active" or "Suspended") || store.State is not ("Active" or "Suspended"))
            return BadRequest(ApiResponse<object>.Fail("Only ready stores can be activated or suspended.", 400));
        store.State = request.State; await _platform.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { store.Id, store.State }));
    }

    public sealed class ApproveRequest
    {
        public int Version { get; set; }
        public bool PaymentVerified { get; set; }
        [MaxLength(200)] public string PaymentReference { get; set; } = "";
        [Required, MaxLength(253)] public string Host { get; set; } = "";
        [Required, MaxLength(80)] public string ConnectionKey { get; set; } = "";
    }
    public sealed class LoginRequest
    {
        [Required, MaxLength(100)] public string Username { get; set; } = "";
        [Required, MaxLength(200)] public string Password { get; set; } = "";
    }
    public sealed class CreateStoreRequest
    {
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        [Required, MaxLength(253)] public string Host { get; set; } = "";
        [Required, MaxLength(80)] public string ConnectionKey { get; set; } = "";
        [Required, MaxLength(100)] public string OwnerUsername { get; set; } = "";
        [Required, EmailAddress, MaxLength(200)] public string OwnerEmail { get; set; } = "";
        [Required, MinLength(12), MaxLength(200)] public string OwnerPassword { get; set; } = "";
    }
    public sealed class StateRequest { [Required] public string State { get; set; } = ""; }
}
