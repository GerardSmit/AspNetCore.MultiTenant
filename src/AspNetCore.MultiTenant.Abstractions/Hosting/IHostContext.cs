namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

/// <summary>
/// Provides the host context in which the tenant is running.
/// </summary>
public interface IHostContext
{
    /// <summary>
    /// Gets the host services.
    /// </summary>
    IServiceProvider HostServices { get; }
}
