using System.Security.Claims;
using Microsoft.AspNetCore.Routing;
using Shop.Api.Tenancy;

namespace Shop.Api.Operations;

public sealed class OperationJournalMiddleware
{
    private readonly RequestDelegate _next;
    public OperationJournalMiddleware(RequestDelegate next)=>_next=next;
    public async Task InvokeAsync(HttpContext context,OperationJournal journal,StoreScope store)
    {
        string failure="";int status=0;
        try{await _next(context);status=context.Response.StatusCode;}
        catch(Exception ex){status=500;failure=ex.GetType().Name;throw;}
        finally
        {
            bool management=context.Request.Path.StartsWithSegments("/api/merchant")||context.Request.Path.StartsWithSegments("/api/platform");
            bool write=context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
            if((management&&write)||status>=500)
            {
                string action=(context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText??"unmatched-api-route";
                await journal.WriteAsync(store.Store?.Id??"",context.User.FindFirstValue("actor_id")??context.User.FindFirstValue(ClaimTypes.NameIdentifier)??"anonymous",context.User.FindFirstValue(ClaimTypes.Role)??"anonymous",action,context.Request.Method,status,context.TraceIdentifier,failure);
                if(status>=500)context.Items["OperationErrorRecorded"]=true;
            }
        }
    }
}
