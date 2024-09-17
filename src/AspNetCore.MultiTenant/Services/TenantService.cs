using System.Collections.Concurrent;
using System.Diagnostics;
using GerardSmit.AspNetCore.MultiTenant.Builder;
using GerardSmit.AspNetCore.MultiTenant.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GerardSmit.AspNetCore.MultiTenant.Services;

public sealed class TenantService(
    IServiceProvider hostServiceProvider,
    IEnumerable<ITenantInitializer> initializers
) : ITenantService
{
    private readonly ConcurrentDictionary<string, TenantContext> _tenantServiceProviders = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IHostEnvironment _hostEnvironment = hostServiceProvider.GetRequiredService<IHostEnvironment>();

    public IEnumerable<ITenantContext> ActiveTenants => _tenantServiceProviders.Values;

    public ValueTask<ITenantContext> GetOrStartTenantAsync(string code)
    {
        if (!_tenantServiceProviders.TryGetValue(code, out var tenant))
        {
            return InitializeAsync(code);
        }

        tenant.LastAccessed = DateTimeOffset.UtcNow;
        return new ValueTask<ITenantContext>(tenant);
    }

    public async ValueTask StopAsync(string code)
    {
        if (!_tenantServiceProviders.TryRemove(code, out var tenant))
        {
            return;
        }

        tenant.State = TenantState.Stopping;

        var hostedServices = tenant.Services.GetServices<IHostedService>().ToArray();
        var lifecycleServices = hostedServices.OfType<IHostedLifecycleService>().ToArray();
        var cancellationToken = CancellationToken.None;

        foreach (var hostedService in lifecycleServices)
        {
            await hostedService.StoppingAsync(cancellationToken);
        }

        foreach (var hostedService in hostedServices)
        {
            await hostedService.StopAsync(cancellationToken);
        }

        foreach (var hostedService in lifecycleServices)
        {
            await hostedService.StoppedAsync(cancellationToken);
        }

        tenant.State = TenantState.Stopped;

        switch (tenant.Services)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    private async ValueTask<ITenantContext> InitializeAsync(string code)
    {
        await _semaphore.WaitAsync();

        ServiceProvider serviceProvider;

        TenantContext? tenant;

        try
        {
            if (_tenantServiceProviders.TryGetValue(code, out tenant))
            {
                return tenant;
            }

            tenant = new TenantContext(code);

            var services = new ServiceCollection();
            var builder = new TenantBuilder(services, code, hostServiceProvider);

            services.AddSingleton<TenantRequestDelegates>();

            foreach (var initializer in initializers)
            {
                await initializer.InitializeAsync(builder);
            }

            services.TryAddSingleton(new DiagnosticListener("Microsoft.AspNetCore"));
            services.AddLogging();
            services.AddOptions();

            services.TryAddSingleton<ITenantContext>(tenant);
            services.TryAddSingleton<IHostEnvironment, TenantHostEnvironment>();
            services.TryAddSingleton<IHostContext>(new HostContext(hostServiceProvider));
            services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();

            var webHostEnvironment = new TenantWebHostEnvironment(_hostEnvironment);
            services.TryAddSingleton<IWebHostEnvironment>(webHostEnvironment);
            services.TryAddSingleton<IHostEnvironment>(webHostEnvironment);

            serviceProvider = services.BuildServiceProvider();
            tenant.Services = serviceProvider;

            _tenantServiceProviders.TryAdd(code, tenant);
        }
        finally
        {
            _semaphore.Release();
        }

        var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        var lifecycleServices = hostedServices.OfType<IHostedLifecycleService>().ToArray();
        var cancellationToken = CancellationToken.None;

        foreach (var hostedService in lifecycleServices)
        {
            await hostedService.StartingAsync(cancellationToken);
        }

        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(cancellationToken);
        }

        foreach (var hostedService in lifecycleServices)
        {
            await hostedService.StartedAsync(cancellationToken);
        }

        tenant.State = TenantState.Running;

        return tenant;
    }
}