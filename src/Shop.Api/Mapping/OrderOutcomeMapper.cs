using Microsoft.AspNetCore.Mvc;
using Shop.Application.Orders;
using Shop.BLL.Response_VM;
namespace Shop.Api.Mapping;

public static class OrderOutcomeMapper
{
    public static ActionResult<ApiResponse<bool>> Map(FulfillmentOutcome outcome) => outcome.Kind switch
    {
        FulfillmentOutcomeKind.Success => new OkObjectResult(ApiResponse<bool>.Ok(true)),
        FulfillmentOutcomeKind.NotFound => new NotFoundObjectResult(ApiResponse<bool>.Fail(outcome.Message!, 404)),
        FulfillmentOutcomeKind.Invalid => new BadRequestObjectResult(ApiResponse<bool>.Fail(outcome.Message!, 400)),
        _ => new ConflictObjectResult(ApiResponse<bool>.Fail(outcome.Message!, 409))
    };
}
