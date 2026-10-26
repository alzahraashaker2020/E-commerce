using Microsoft.EntityFrameworkCore;
using Shop.BLL.Response_VM;

namespace Shop.Api.Tenancy;

public sealed class StoreResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public StoreResolutionMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context, PlatformContext platform, StoreScope scope, DeploymentProfile profile)
    {
        if(profile.SaasEnabled && context.Request.Path.StartsWithSegments("/api/merchant/operations"))
        { context.Response.StatusCode=404; return; }
        if (!profile.SaasEnabled && (context.Request.Path.StartsWithSegments("/api/platform") || context.Request.Path.StartsWithSegments("/api/merchant/subscription")))
        { context.Response.StatusCode=404; return; }
        if(context.Request.Path.Equals(new PathString("/api/deployment")) && HttpMethods.IsGet(context.Request.Method))
        { await _next(context); return; }
        if (context.Request.Path.StartsWithSegments("/api/platform") || context.Request.Path.StartsWithSegments("/swagger"))
        { await _next(context); return; }
        string host = context.Request.Host.Host.ToLowerInvariant();
        scope.Store = await profile.Stores(platform).AsNoTracking().SingleOrDefaultAsync(s => s.Host == host);
        if (scope.Store is null || scope.Store.State != "Active")
        {
            context.Response.StatusCode = scope.Store is null ? 404 : 403;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Store unavailable", context.Response.StatusCode));
            return;
        }
        var subscription = profile.SaasEnabled ? await platform.Subscriptions.AsNoTracking().SingleOrDefaultAsync(s => s.StoreId == scope.StoreId) : null;
        scope.SecurityVersion=await context.RequestServices.GetRequiredService<Shop.Api.Operations.OperationJournal>().ReadSecurityVersionAsync(scope.StoreId);
        scope.Subscription = subscription;
        bool write = context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
        bool merchantWrite = context.Request.Path.StartsWithSegments("/api/merchant") && !context.Request.Path.StartsWithSegments("/api/merchant/auth");
        if (write && (merchantWrite || context.Request.Path.StartsWithSegments("/api/checkout")) &&
            profile.SaasEnabled && scope.Store.SubscriptionRequired && !SubscriptionAccess.IsCurrent(subscription))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("An active store subscription is required. Contact the platform to activate or renew your plan.", 403));
            return;
        }
        string? neededFeature=context.Request.Path.StartsWithSegments("/api/merchant/discounts")?"Discounts":
            (context.Request.Path.StartsWithSegments("/api/storefront/subscribe") || context.Request.Path.StartsWithSegments("/api/merchant/storefront/subscribers"))?"Newsletter":
            (write && context.Request.Path.Equals(new PathString("/api/merchant/storefront")))?"StorefrontDesign":null;
        if(neededFeature is not null && !PlanFeatures.Allows(profile,scope,neededFeature))
        {
            context.Response.StatusCode=403;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("This feature is not included in the store subscription.",403));return;
        }
        if ((context.Request.Path.StartsWithSegments("/api/cart") ||
             (context.Request.Path.StartsWithSegments("/api/checkout") && write)) &&
            await context.RequestServices.GetRequiredService<StoreCapabilities>().BusinessModeAsync()=="Catalog")
        {
            context.Response.StatusCode=403;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("This store is a display catalog. Online purchasing is unavailable.",403));
            return;
        }
        string path = context.Request.Path.Value ?? "";
        string normalized = Uri.UnescapeDataString(path).Replace('\\', '/');
        if (normalized.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) && normalized != path)
        { context.Response.StatusCode = 404; return; }
        // Uploaded images are public storefront assets, but only from this host's store.
        if (context.Request.Path.StartsWithSegments("/uploads", out var remaining))
        {
            if (Path.GetExtension(remaining.Value ?? "").ToLowerInvariant() is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            { context.Response.StatusCode = 404; return; }
            bool legacy = scope.Store.ConnectionKey == "DefaultStore" && !remaining.Value!.StartsWith("/stores/", StringComparison.OrdinalIgnoreCase);
            if (!legacy && !remaining.StartsWithSegments("/stores/" + scope.StoreId))
            { context.Response.StatusCode = 404; return; }
        }
        await _next(context);
    }
}
