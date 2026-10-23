using Microsoft.AspNetCore.Mvc;
using Shop.BLL.IRepositories;
using Shop.BLL.Response_VM;
using Shop.BLL.Security;
using Shop.DAL.Models;
using Shop.DAL.ViewModels;
using Shop.SL.TokenManager;

namespace Shop.Api.Controllers;

/// <summary>
/// Customer authentication: local registration/login plus social login
/// (Google/Facebook). Lives at <c>api/account</c> and is deliberately NOT
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
    /// Social login (Google/Facebook). This is the server-side plumbing: a real
    /// deployment verifies <paramref name="request"/>.Credential with the
    /// provider and extracts the verified email + subject id. Here, for the
    /// demonstration, the credential is treated as "provider:externalId:email"
    /// so the full account-linking flow can be exercised without live provider
    /// secrets. Find-or-create links the external identity to a customer.
    /// </summary>
    /// <param name="request">The social login payload.</param>
    [HttpPost("social")]
    public async Task<ActionResult<ApiResponse<CustomerAuthResponse>>> SocialLogin(
        [FromBody] SocialLoginRequest request)
    {
        // Only providers we actually support (Instagram is intentionally not a
        // login provider — see docs). Reject anything else.
        if (request.Provider != "Google" && request.Provider != "Facebook")
        {
            return BadRequest(ApiResponse<CustomerAuthResponse>.Fail("Unsupported provider", 400));
        }

        // DEMO credential parsing. REAL implementation: verify the provider
        // token server-side (Google tokeninfo / Facebook debug_token) and read
        // the verified subject id + email from the provider's response.
        (string externalId, string email, string name) = ParseDemoCredential(request.Credential, request.Provider);
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<CustomerAuthResponse>.Fail("Invalid social credential", 400));
        }

        // Find-or-create: match on the external identity first, then by email
        // (to link a social login to an existing local account), else create.
        Customer? customer = await _repo._Customer.GetByExternalAsync(request.Provider, externalId)
            ?? await _repo._Customer.GetByEmailAsync(email);

        if (customer is null)
        {
            customer = new Customer
            {
                Email = email,
                FullName = name,
                ExternalProvider = request.Provider,
                ExternalId = externalId,
                IsRegistered = true,
                CreatedAt = DateTime.UtcNow,
                IsDemoData = false
            };
            _repo._Customer.Add(customer);
            _repo.Save();
        }
        else if (string.IsNullOrEmpty(customer.ExternalProvider))
        {
            // Link the external identity to the existing account.
            customer.ExternalProvider = request.Provider;
            customer.ExternalId = externalId;
            customer.IsRegistered = true;
            _repo._Customer.Update(customer);
            _repo.Save();
        }

        return Ok(ApiResponse<CustomerAuthResponse>.Ok(BuildAuthResponse(customer)));
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

    /// <summary>
    /// DEMO-ONLY credential parser. Accepts "externalId:email:name" (name
    /// optional). Replace with real provider token verification in production.
    /// </summary>
    private static (string ExternalId, string Email, string Name) ParseDemoCredential(string credential, string provider)
    {
        string[] parts = credential.Split(':');
        string externalId = parts.Length > 0 ? parts[0] : string.Empty;
        string email = parts.Length > 1 ? parts[1] : string.Empty;
        string name = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])
            ? parts[2]
            : $"{provider} User";
        return (externalId, email, name);
    }
}
