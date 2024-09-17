using GerardSmit.AspNetCore.MultiTenant.Builder;
using GerardSmit.AspNetCore.MultiTenant.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GerardSmit.AspNetCore.MultiTenant;

public static class ServiceExtensions
{
    public static IServiceCollection AddMultiTenant(
        this IServiceCollection rootServices,
        Action<TenantHostBuilder>? configure = null)
    {
        rootServices.TryAddSingleton<ITenantService, TenantService>();
        rootServices.TryAdd(new ServiceDescriptor(typeof(IHostedService), typeof(TenantManagerHostedService), ServiceLifetime.Singleton));
        configure?.Invoke(new TenantHostBuilder(rootServices));

        return rootServices;
    }
}

