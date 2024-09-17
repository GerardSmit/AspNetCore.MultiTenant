using Microsoft.Extensions.DependencyInjection;

namespace GerardSmit.AspNetCore.MultiTenant.Builder;

/// <summary>
/// Defines interface that provides the mechanisms to configure an tenant's services.
/// </summary>
public interface ITenantBuilder
{
    /// <summary>
    /// Gets the items of the tenant builder.
    /// </summary>
    Dictionary<object, object?> Items { get; }

    /// <summary>
    /// Gets the host service provider.
    /// </summary>
    IServiceProvider HostServices { get; }

    /// <summary>
    /// Gets the tenant registered services.
    /// </summary>
    IServiceCollection TenantServices { get; set; }

    /// <summary>
    /// Gets the tenant code that is being initialized.
    /// </summary>
    string TenantCode { get; }
}
