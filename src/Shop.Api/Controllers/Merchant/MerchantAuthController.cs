using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using Shop.SL.TokenManager;

namespace Shop.Api.Controllers.Merchant;

/// <summary>
/// Merchant authentication endpoint at
/// <c>api/merchant/auth/login</c> and is deliberately NOT <c>[Authorize]</c> so callers
/// can obtain a token. Issues a JWT on valid credentials.
/// </summary>
[ApiController]
[Route("api/merchant/auth")]
public class MerchantAuthController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Creates a new <see cref="AuthController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="passwordHasher">Password hashing/verification service.</param>
    /// <param name="tokenService">JWT token issuer.</param>
    public MerchantAuthController(
        IRepoWrapper repo,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService)
    {
        _repo = repo;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Authenticates an admin user and returns a signed JWT. Returns 401 for an
    /// unknown user or bad password (no user/password distinction) and 403 when
    /// the account is inactive.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Login(
        [FromBody] AuthenticateRequest request)
    {
        AdminUser? user = await _repo._AdminUser.GetByUsernameAsync(request.Username);

        // Unknown user or bad password both return the same 401 so the response
        // does not reveal which part was wrong.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return Unauthorized(ApiResponse<AuthenticateResponse>.Fail("Invalid credentials", 401));
        }

        if (!user.IsActive)
        {
            return StatusCode(403, ApiResponse<AuthenticateResponse>.Fail("Account disabled", 403));
        }

        if (user.Role?.Name is not ("Admin" or "Merchant"))
            return StatusCode(403, ApiResponse<AuthenticateResponse>.Fail("Merchant access required", 403));

        TokenVM token = _tokenService.GenerateToken(user);

        var response = new AuthenticateResponse
        {
            Token = token.Token,
            Expires = token.Expires,
            Username = user.Username,
            Role = "Merchant"
        };

        return Ok(ApiResponse<AuthenticateResponse>.Ok(response));
    }
}
