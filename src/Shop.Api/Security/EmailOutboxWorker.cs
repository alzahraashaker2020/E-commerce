using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;
using Shop.BLL.Email;

namespace Shop.Api.Security;

public sealed class EmailOutboxWorker:BackgroundService
{
    private readonly IServiceScopeFactory scopes;
    private readonly ILogger<EmailOutboxWorker> logger;
    public EmailOutboxWorker(IServiceScopeFactory scopes,ILogger<EmailOutboxWorker> logger){this.scopes=scopes;this.logger=logger;}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var registry=scopes.CreateAsyncScope();
                var profile=registry.ServiceProvider.GetRequiredService<DeploymentProfile>();
                var stores=await profile.Stores(registry.ServiceProvider.GetRequiredService<PlatformContext>()).AsNoTracking().Where(s=>s.State=="Active"||s.State=="Suspended").ToListAsync(stoppingToken);
                foreach(var store in stores)
                {
                    if(stoppingToken.IsCancellationRequested)break;
                    try
                    {
                        await using var scope=scopes.CreateAsyncScope();
                        scope.ServiceProvider.GetRequiredService<StoreScope>().Store=store;
                        var outbox=scope.ServiceProvider.GetRequiredService<EmailOutbox>();
                        var transport=scope.ServiceProvider.GetRequiredService<EmailService>();
                        foreach(var entry in await outbox.ReadAsync(true))
                        {if(stoppingToken.IsCancellationRequested)break;await outbox.DeliverAsync(entry,transport);}
                    }
                    catch(Exception ex){logger.LogWarning("Email outbox processing delayed for store {StoreId} ({FailureType}).",store.Id,ex.GetType().Name);}
                }
            }
            catch(Exception) when(!stoppingToken.IsCancellationRequested){logger.LogWarning("Email outbox registry is temporarily unavailable.");}
            try{await Task.Delay(1000,stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
