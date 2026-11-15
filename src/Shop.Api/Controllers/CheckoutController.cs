using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shop.Application.Checkout;
using Shop.Api.Adapters;
using Shop.Api.Mapping;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.ViewModels;
namespace Shop.Api.Controllers;

[ApiController, Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly CheckoutService _checkout; private readonly OrderTrackingAccess _tracking;
    public CheckoutController(CheckoutService checkout, OrderTrackingAccess tracking) { _checkout = checkout; _tracking = tracking; }
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderConfirmationResponse>>> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        int? customer = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        var actor = new CheckoutActor(Request.Headers.ContainsKey("Authorization"), User.Identity?.IsAuthenticated == true, User.IsInRole("Customer"), customer);
        // Hash the original API payload BEFORE translating contracts. Never canonicalize it.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        var result = await _checkout.PlaceAsync(CheckoutBoundary.Copy<CheckoutInput>(request), actor, hash, HttpContext.RequestAborted);
        if (result.NotCreated) Response.Headers["X-Checkout-Outcome"] = "NotCreated";
        return CheckoutOutcomeMapper.Map(result, _tracking);
    }
    [HttpPost("quote")]
    public async Task<ActionResult<ApiResponse<CheckoutQuote>>> Quote(CheckoutQuoteRequest request) => CheckoutOutcomeMapper.Map(await _checkout.QuoteAsync(CheckoutBoundary.Copy<QuoteInput>(request), HttpContext.RequestAborted));
}
