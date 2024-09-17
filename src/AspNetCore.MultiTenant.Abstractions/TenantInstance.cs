using GerardSmit.AspNetCore.MultiTenant.Options;

namespace GerardSmit.AspNetCore.MultiTenant;

public interface ITenantContext
{
    /// <summary>
    /// Code of the tenant.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Root service provider for the tenant.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Current state of the tenant.
    /// </summary>
    TenantState State { get; }

    /// <summary>
    /// UTC time when the tenant was last accessed.
    /// When the tenant hasn't been accessed for a certain period (see <see cref="TenantOptions.StopInterval"/>) , it will be stopped.
    /// </summary>
    DateTimeOffset LastAccessed { get; }
}