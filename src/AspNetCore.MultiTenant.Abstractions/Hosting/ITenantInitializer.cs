using GerardSmit.AspNetCore.MultiTenant.Builder;

namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

/// <summary>
/// Represents a service that initializes a tenant. It's possible to have multiple initializers.
/// </summary>
public interface ITenantInitializer
{
    /// <summary>
    /// Initializes the tenant.
    /// </summary>
    /// <param name="builder">The tenant builder.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    ValueTask InitializeAsync(ITenantBuilder builder);
}
