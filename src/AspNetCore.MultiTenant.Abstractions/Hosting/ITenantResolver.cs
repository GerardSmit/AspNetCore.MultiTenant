using Microsoft.AspNetCore.Http;

namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

/// <summary>
/// Resolves the tenant code from the current HTTP context.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant code from the current HTTP context.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <returns>The tenant code.</returns>
    ValueTask<string?> ResolveAsync(HttpContext context);
}