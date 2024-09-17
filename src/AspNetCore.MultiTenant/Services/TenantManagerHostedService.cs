using GerardSmit.AspNetCore.MultiTenant.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GerardSmit.AspNetCore.MultiTenant.Services;

internal class TenantManagerHostedService(
    ITenantService tenantService,
    IOptions<TenantOptions> options,
    ILogger<TenantManagerHostedService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (!stoppingToken.IsCancellationRequested)
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await StopTenantsAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private async Task StopTenantsAsync()
    {
        try
        {
            var interval = options.Value.StopInterval;

            if (interval == TimeSpan.Zero || interval == TimeSpan.MaxValue)
            {
                // Ignore: stopping is disabled.
                // It's possible that the configuration will change, so we don't stop the timer.
                return;
            }

            var now = DateTimeOffset.UtcNow;

            foreach (var tenant in tenantService.ActiveTenants)
            {
                if (now - tenant.LastAccessed < interval)
                {
                    continue;
                }

                logger.LogDebug("Stopping tenant {TenantCode} due to inactivity", tenant.Code);
                await tenantService.StopAsync(tenant.Code);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while stopping tenants");
        }
    }
}
