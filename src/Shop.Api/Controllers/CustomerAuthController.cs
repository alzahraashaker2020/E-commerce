using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using Shop.SL.TokenManager;

namespace Shop.Api.Controllers;

/// <summary>
/// Customer authentication: local registration/login. Social sign-in is unavailable
/// until provider verification is implemented. Lives at <c>api/account</c> and is deliberately NOT
/// <c>[Authorize]</c> so visitors can create a session.
/// </summary>
[ApiController]
[Route("api/account")]
public class CustomerAuthController : ControllerBase
{
    private readonly IRepoWrapper _repo;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Creates a new <see cref="CustomerAuthController"/>.
    /// </summary>
    /// <param name="repo">Unit-of-work repository wrapper.</param>
    /// <param name="passwordHasher">Password hashing/verification service.</param>
    /// <param name="tokenService">JWT token issuer.</param>
    public CustomerAuthController(
        IRepoWrapper repo,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService)
    {
        _repo = repo;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registers a new local customer account. Rejects a duplicate email with
    /// 409. Hashes the password; never stores it in plaintext.
    /// </summary>
    /// <param name="request">The registration payload.</param>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<CustomerAuthResponse>>> Register(
        [FromBody] CustomerRegisterRequest request)
    {
        if (await _repo._Customer.GetByEmailAsync(request.Email) is not null)
        {
            return Conflict(ApiResponse<CustomerAuthResponse>.Fail("An account with that email already exists", 409));
        }

        var customer = new Customer
        {
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            Phone = request.Phone,
            MarketingConsent = request.MarketingConsent,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsRegistered = true,
            CreatedAt = DateTime.UtcNow,
            IsDemoData = false
        };

        _repo._Customer.Add(customer);
        _repo.Save();

        return Ok(ApiResponse<CustomerAuthResponse>.Ok(BuildAuthResponse(customer)));
    }

    /// <summary>
    /// Authenticates a local customer and returns a JWT. Returns 401 for an
    /// unknown email, a social-only account (no local password), or a bad
    /// password — all with the same message so nothing is leaked.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<CustomerAuthResponse>>> Login(
        [FromBody] CustomerLoginRequest request)
    {
        Customer? customer = await _repo._Customer.GetByEmailAsync(request.Email);

        // Unknown account, social-only account (no hash), or wrong password all
        // return the same 401 so the response reveals nothing.
        if (customer is null
            || string.IsNullOrEmpty(customer.PasswordHash)
            || !_passwordHasher.Verify(customer.PasswordHash, request.Password))
        {
            return Unauthorized(ApiResponse<CustomerAuthResponse>.Fail("Invalid email or password", 401));
        }

        return Ok(ApiResponse<CustomerAuthResponse>.Ok(BuildAuthResponse(customer)));
    }

    /// <summary>
    /// Rejects social sign-in until provider token verification is implemented.
    /// Never accepts client-supplied identity data as proof of authentication.
    /// </summary>
    [HttpPost("social")]
    public ActionResult<ApiResponse<CustomerAuthResponse>> SocialLogin(
        [FromBody] SocialLoginRequest request)
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            ApiResponse<CustomerAuthResponse>.Fail(
                "Social sign-in is currently unavailable. Please sign in with email and password.",
                StatusCodes.Status503ServiceUnavailable));
    }

    /// <summary>
    /// Builds the auth response (token + profile summary) for a customer.
    /// </summary>
    /// <param name="customer">The authenticated customer.</param>
    private CustomerAuthResponse BuildAuthResponse(Customer customer)
    {
        TokenVM token = _tokenService.GenerateCustomerToken(customer);
        return new CustomerAuthResponse
        {
            Token = token.Token,
            Expires = token.Expires,
            CustomerId = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email
        };
    }

}
