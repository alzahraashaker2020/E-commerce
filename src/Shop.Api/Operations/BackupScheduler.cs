using Microsoft.EntityFrameworkCore;
using Shop.Api.Tenancy;

namespace Shop.Api.Operations;

public sealed class BackupScheduler:BackgroundService
{
    private readonly IServiceScopeFactory _scopes;private readonly OperationJournal _journal;private readonly StoreBackups _backups;
    private readonly int _poll;
    public BackupScheduler(IServiceScopeFactory scopes,OperationJournal journal,StoreBackups backups,IConfiguration config){_scopes=scopes;_journal=journal;_backups=backups;_poll=Math.Clamp(config.GetValue<int?>("Operations:SchedulerPollSeconds")??60,1,60);}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var policy=await _journal.ReadPolicyAsync();
                if(policy.Enabled)
                {
                    await using var scope=_scopes.CreateAsyncScope();var platform=scope.ServiceProvider.GetRequiredService<PlatformContext>();
                    var profile=scope.ServiceProvider.GetRequiredService<DeploymentProfile>();
                    var ids=await profile.Stores(platform).Where(s=>s.State=="Active"||s.State=="Suspended").Select(s=>s.Id).ToListAsync(stoppingToken);
                    if(profile.SaasEnabled)ids.Insert(0,"platform");
                    foreach(var id in ids)
                    {
                        var previous=_backups.List(id).FirstOrDefault();if(previous is not null&&previous.LastVerificationState!="Failed"&&previous.CreatedAtUtc>DateTime.UtcNow.AddHours(-policy.IntervalHours))continue;
                        try{await _backups.CreateAsync(id,stoppingToken);await _journal.WriteAsync(id,"scheduler","PlatformAdmin","ScheduledBackupCreated","JOB",200,Guid.NewGuid().ToString("N"));}
                        catch(Exception ex)when(!stoppingToken.IsCancellationRequested){await _journal.WriteAsync(id,"scheduler","PlatformAdmin","ScheduledBackupFailed","JOB",500,Guid.NewGuid().ToString("N"),ex.GetType().Name);}
                    }
                }
            }
            catch(Exception ex)when(!stoppingToken.IsCancellationRequested){await _journal.WriteAsync("","scheduler","PlatformAdmin","BackupSchedulerFailed","JOB",500,Guid.NewGuid().ToString("N"),ex.GetType().Name);}
            try{await Task.Delay(TimeSpan.FromSeconds(_poll),stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
