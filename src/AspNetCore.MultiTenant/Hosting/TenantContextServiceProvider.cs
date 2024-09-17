using System.Collections.Concurrent;
using GerardSmit.AspNetCore.MultiTenant.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

internal sealed class TenantContextServiceProvider(
    IServiceProvider tenantServiceProvider,
    IServiceProvider hostServiceProvider,
    ILogger<TenantContextServiceProvider> logger
) : IServiceProvider
{
    private static readonly ConcurrentDictionary<Type, bool> NotifiedTypes = new();

    public object? GetService(Type serviceType)
    {
        var service = tenantServiceProvider.GetService(serviceType);

        if (service != null)
        {
            return service;
        }

        var options = hostServiceProvider.GetRequiredService<IOptions<TenantOptions>>().Value;

        var isAllowedType = options.RedirectScopeTypes.Contains(serviceType) ||
                            serviceType.IsGenericType && options.RedirectScopeTypes.Contains(serviceType.GetGenericTypeDefinition());

        if (isAllowedType)
        {
            // The type is allowed to be redirected to the host service provider
        }
        else if (options.AllowFallbackToHostServiceProvider)
        {
            // Inform the user that the service was not found in the tenant service provider
            if (NotifiedTypes.TryAdd(serviceType, true))
            {
                logger.LogDebug(
                    "Service of type '{ServiceType}' not found in tenant service provider, falling back to root service provider",
                    serviceType
                );
            }
        }

        return hostServiceProvider.GetService(serviceType);
    }
}