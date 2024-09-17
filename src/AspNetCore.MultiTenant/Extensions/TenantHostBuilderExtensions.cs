using System.Collections.Concurrent;
using GerardSmit.AspNetCore.MultiTenant.Builder;
using GerardSmit.AspNetCore.MultiTenant.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GerardSmit.AspNetCore.MultiTenant;

public delegate string PrefixLogger(string tenantCode, string categoryName);

public static class TenantHostBuilderExtensions
{
    /// <summary>
    /// Redirect the logging to the host logger.
    /// </summary>
    /// <param name="builder">The <see cref="TenantHostBuilder"/>.</param>
    /// <param name="formatCategoryName">The function to format the category name. By default, it will prefix the tenant code to the category name.</param>
    /// <returns>The instance of <see cref="builder"/>.</returns>
    public static TenantHostBuilder RedirectLoggingToHost(this TenantHostBuilder builder, PrefixLogger? formatCategoryName = null)
    {
        formatCategoryName ??= (tenantCode, categoryName) => $"[{tenantCode}] {categoryName}";

        builder.ConfigureTenant(b =>
        {
            b.TenantServices.AddLogging();
            b.TenantServices.AddSingleton<ILoggerProvider>(sp => ActivatorUtilities.CreateInstance<RedirectLoggingToHostTenantInitializer>(sp, formatCategoryName));
        });
        return builder;
    }

    private class RedirectLoggingToHostTenantInitializer(IHostContext hostContext, ITenantContext tenantContext, PrefixLogger prefix) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<(string, string), string> _categoryNames = new();
        private readonly ILoggerFactory _loggerFactory = hostContext.HostServices.GetRequiredService<ILoggerFactory>();

        public ILogger CreateLogger(string categoryName)
        {
            var tenantCategoryName = _categoryNames.TryGetValue((tenantContext.Code, categoryName), out var value)
                ? value
                : _categoryNames.GetOrAdd((tenantContext.Code, categoryName), CreateTenantCategoryName);

            return _loggerFactory.CreateLogger(tenantCategoryName);
        }

        private string CreateTenantCategoryName((string, string) arg)
        {
            var (tenantCode, categoryName) = arg;

            return prefix(tenantCode, categoryName);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Resolve the tenant by the host name of the request.
    /// </summary>
    /// <param name="builder">The <see cref="TenantHostBuilder"/>.</param>
    /// <param name="includePort">Whether to include the port in the host name.</param>
    /// <returns>The instance of <see cref="builder"/>.</returns>
    public static TenantHostBuilder ResolveTenantByHost(this TenantHostBuilder builder, bool includePort = true)
    {
        builder.HostServices.AddSingleton<ITenantResolver>(new HostResolver(includePort));
        return builder;
    }

    private class HostResolver(bool includePort) : ITenantResolver
    {
        public ValueTask<string?> ResolveAsync(HttpContext context)
        {
            var host = context.Request.Host;

            if (!includePort)
            {
                return new ValueTask<string?>(host.Host);
            }

            if ((host.Port == 80 && context.Request.Scheme == "http") ||
                (host.Port == 443 && context.Request.Scheme == "https"))
            {
                return new ValueTask<string?>(host.Host);
            }

            return new ValueTask<string?>(host.ToString());
        }
    }
}
