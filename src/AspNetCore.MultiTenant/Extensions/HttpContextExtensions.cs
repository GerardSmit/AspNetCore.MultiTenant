using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GerardSmit.AspNetCore.MultiTenant;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current tenant from the <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The current tenant.</returns>
    public static ITenantContext GetTenant(this HttpContext context)
    {
        return context.RequestServices.GetRequiredService<ITenantContext>();
    }
}
