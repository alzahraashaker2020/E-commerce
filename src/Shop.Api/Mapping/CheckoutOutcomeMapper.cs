using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Shop.Application.Checkout;
using Shop.Api.Adapters;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;
using Shop.DAL.ViewModels;
namespace Shop.Api.Mapping;

public static class CheckoutOutcomeMapper
{
    public static ActionResult<ApiResponse<OrderConfirmationResponse>> Map(CheckoutOutcome result, OrderTrackingAccess tracking)
    {
        if (result.ReplayJson is not null) { var saved = JsonSerializer.Deserialize<ApiResponse<OrderConfirmationResponse>>(result.ReplayJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!; if (saved.Data is not null) saved.Data.TrackingToken = tracking.Create(saved.Data.OrderNumber); return new OkObjectResult(saved); }
        if (result.Kind == CheckoutOutcomeKind.Success) return new OkObjectResult(ApiResponse<OrderConfirmationResponse>.Ok(CheckoutBoundary.Copy<OrderConfirmationResponse>(result.Confirmation!), result.Message));
        int status = result.Kind switch { CheckoutOutcomeKind.Invalid => 400, CheckoutOutcomeKind.Unauthenticated => 401, CheckoutOutcomeKind.Forbidden => 403, _ => 409 };
        return new ObjectResult(ApiResponse<OrderConfirmationResponse>.Fail(result.Message!, status)) { StatusCode = status };
    }
    public static ActionResult<ApiResponse<CheckoutQuote>> Map(QuoteOutcome result)
    {
        if (result.Kind == CheckoutOutcomeKind.Success) return new OkObjectResult(ApiResponse<CheckoutQuote>.Ok(CheckoutBoundary.Copy<CheckoutQuote>(result.Quote!), result.Message));
        int status = result.Kind == CheckoutOutcomeKind.Invalid ? 400 : 409; return new ObjectResult(ApiResponse<CheckoutQuote>.Fail(result.Message!, status)) { StatusCode = status };
    }
}
