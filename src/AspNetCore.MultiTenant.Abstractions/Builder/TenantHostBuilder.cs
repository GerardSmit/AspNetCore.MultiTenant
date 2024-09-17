using Microsoft.Extensions.DependencyInjection;

namespace GerardSmit.AspNetCore.MultiTenant.Builder;

/// <summary>
/// Represents a builder for configuring the tenant services.
/// </summary>
/// <param name="hostServices">The host services.</param>
public class TenantHostBuilder(IServiceCollection hostServices)
{
    /// <summary>
    /// Gets the services of the host.
    /// </summary>
    public IServiceCollection HostServices { get; } = hostServices;
}
