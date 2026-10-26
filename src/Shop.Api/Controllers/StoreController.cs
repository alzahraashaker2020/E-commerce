using Microsoft.AspNetCore.Mvc;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers;

[ApiController, Route("api/store")]
public sealed class StoreController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices] StoreScope scope, [FromServices] DeploymentProfile profile, [FromServices] StoreCapabilities capabilities)
    {
        Response.Headers["Cache-Control"]="no-store";
        var businessMode=await capabilities.BusinessModeAsync();
        return Ok(ApiResponse<object>.Ok(new { scope.Store!.Id, scope.Store.Name,
            Features=PlanFeatures.All.Where(f=>PlanFeatures.Allows(profile,scope,f)).ToArray(), DeploymentMode=profile.Mode, profile.SaasEnabled, BusinessMode=businessMode,
            CommerceEnabled=businessMode=="Commerce", OperationsEnabled=!profile.SaasEnabled,
            BusinessModeLocked=profile.BusinessModeOverride is not null }));
    }
}
