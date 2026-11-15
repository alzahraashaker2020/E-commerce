using Microsoft.AspNetCore.Mvc;
using Shop.Application.Provisioning;
using Shop.BLL.Response_VM;
namespace Shop.Api.Mapping;

public static class ProvisioningOutcomeMapper
{
    public static IActionResult Map(ProvisioningOutcome result)
    {
        if (result.Kind == ProvisioningOutcomeKind.Success) return new OkObjectResult(ApiResponse<object>.Ok(result.Store!));
        int status = result.Kind switch { ProvisioningOutcomeKind.Invalid => 400, ProvisioningOutcomeKind.NotFound => 404, ProvisioningOutcomeKind.Conflict => 409, _ => 500 };
        if (result.Bare) return new StatusCodeResult(status);
        return new ObjectResult(ApiResponse<object>.Fail(result.Message!, status)) { StatusCode = status };
    }
}
