namespace GerardSmit.AspNetCore.MultiTenant.Services;

/// <summary>
/// Manages the tenants in the application.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the active tenants.
    /// </summary>
    IEnumerable<ITenantContext> ActiveTenants { get; }

    /// <summary>
    /// Gets or starts a tenant.
    /// </summary>
    /// <param name="code">The tenant code.</param>
    /// <returns>The tenant instance.</returns>
    ValueTask<ITenantContext> GetOrStartTenantAsync(string code);

    /// <summary>
    /// Stops a tenant.
    /// </summary>
    /// <param name="code">The tenant code.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask StopAsync(string code);
}
