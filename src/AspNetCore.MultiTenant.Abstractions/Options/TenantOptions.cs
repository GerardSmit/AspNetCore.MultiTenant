namespace GerardSmit.AspNetCore.MultiTenant.Options;

public class TenantOptions
{
    /// <summary>
    /// Time when a tenant should be stopped if it has not been accessed.
    /// By default, tenants are never stopped.
    /// </summary>
    public TimeSpan StopInterval { get; set; } = TimeSpan.MaxValue;

    /// <summary>
    /// Allow falling back to the host service provider if a service is not found in the tenant service provider.
    /// </summary>
    public bool AllowFallbackToHostServiceProvider { get; set; }

    /// <summary>
    /// Scoped types that should be redirected to the scoped host service provider.
    /// </summary>
    public HashSet<Type> RedirectScopeTypes { get; } = new();
}
