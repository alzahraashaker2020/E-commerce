using System.Text.Json;
using Shop.Application.Checkout;
using Shop.Api.Controllers;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.ViewModels;
namespace Shop.Api.Adapters;

public sealed class CheckoutBoundary : ICheckoutBoundary
{
    private readonly OrderTrackingAccess _tracking; private readonly DeploymentProfile _profile; private readonly StoreScope _scope;
    public CheckoutBoundary(OrderTrackingAccess tracking, DeploymentProfile profile, StoreScope scope) { _tracking = tracking; _profile = profile; _scope = scope; }
    public bool DiscountsAllowed => PlanFeatures.Allows(_profile, _scope, "Discounts");
    public string CreateTrackingProof(string number) => _tracking.Create(number);
    public string NewOrderNumber(DateTime now) => OrderNumberGenerator.Generate(now);
    public string EncodeReceipt(CheckoutConfirmation confirmation, string? message) => JsonSerializer.Serialize(ApiResponse<OrderConfirmationResponse>.Ok(Copy<OrderConfirmationResponse>(confirmation), message), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    // Identically named scalar contract fields are copied only at the HTTP boundary.
    public static T Copy<T>(object value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
