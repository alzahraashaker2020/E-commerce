using System.Text.Json;
using Shop.BLL.Response_VM;

namespace Shop.Api.Middleware;

/// <summary>
/// Global exception-handling middleware. Registered as the outermost layer in
/// the request pipeline so any unhandled exception thrown downstream is caught,
/// logged at Error, and rendered as the standard <see cref="ApiResponse{T}"/>
/// 500 shape. No stack trace is leaked to the caller.
/// </summary>
public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    /// <summary>
    /// Creates a new <see cref="ApiExceptionMiddleware"/>.
    /// </summary>
    /// <param name="next">The next delegate in the request pipeline.</param>
    /// <param name="logger">Logger used to record unhandled exceptions.</param>
    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the next middleware and converts any unhandled exception into
    /// the standard failure envelope.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the full exception (including stack) server-side only.
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, (context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText??"unmatched-api-route");
            if(!context.Items.ContainsKey("OperationErrorRecorded"))
                await context.RequestServices.GetRequiredService<Shop.Api.Operations.OperationJournal>().WriteAsync(
                    context.RequestServices.GetRequiredService<Shop.Api.Tenancy.StoreScope>().Store?.Id??"","anonymous","anonymous",(context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText??"unmatched-api-route",context.Request.Method,500,context.TraceIdentifier,ex.GetType().Name);

            // Return the standard ApiResponse 500 shape; never leak the stack.
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.Headers["X-Error-Reference"]=context.TraceIdentifier;

            ApiResponse<object> body = ApiResponse<object>.Fail("Unexpected error", 500);
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}
