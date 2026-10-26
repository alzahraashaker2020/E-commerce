using Microsoft.AspNetCore.Mvc;
using Shop.Api.Tenancy;
using Shop.BLL.Response_VM;

namespace Shop.Api.Controllers;

[ApiController, Route("api/deployment")]
public sealed class DeploymentController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromServices] DeploymentProfile profile,[FromServices]IConfiguration config)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return Ok(ApiResponse<object>.Ok(new { profile.Mode, profile.SaasEnabled, IsPlatformHost=profile.SaasEnabled && Request.Host.Host.Equals(config["Platform:PublicHost"]??"platform.localhost",StringComparison.OrdinalIgnoreCase) }));
    }
}
