using System.Runtime.CompilerServices;
using GerardSmit.AspNetCore.MultiTenant.Builder;
using GerardSmit.AspNetCore.MultiTenant.Hosting;
using GerardSmit.AspNetCore.MultiTenant.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GerardSmit.AspNetCore.MultiTenant;

public static class ApplicationBuilderExtensions
{
    private static readonly object TenantServiceProvider = new();
    private static readonly object HostServiceProvider = new();
    private static readonly object TenantCode = new();

    public static IApplicationBuilder UseMultiTenant(this IApplicationBuilder app, Action<ITenantApplicationBuilder> configure)
    {
        var middlewareId = Guid.NewGuid();
        var resolver = app.ApplicationServices.GetRequiredService<ITenantResolver>();
        var tenantService = app.ApplicationServices.GetRequiredService<ITenantService>();

        return app.Use(next =>
        {
            return async context =>
            {
                // Get the request delegate for the current tenant
                string? code;

                if (!context.Items.TryGetValue(TenantCode, out var codeObj) || codeObj is null)
                {
                    code = await resolver.ResolveAsync(context);

                    if (code is null)
                    {
                        throw new InvalidOperationException("Tenant code not found.");
                    }

                    context.Items[TenantCode] = code;
                }
                else
                {
                    code = (string)codeObj;
                }

                var tenant = await tenantService.GetOrStartTenantAsync(code);
                var requestDelegates = tenant.Services.GetRequiredService<TenantRequestDelegates>();

                if (!requestDelegates.Delegates.TryGetValue(middlewareId, out var requestDelegate))
                {
                    requestDelegate = await GetRequestDelegate(tenant, requestDelegates, async ctx =>
                    {
                        // Before going to the next middleware, we reset the request services to the root service provider
                        ctx.RequestServices = (IServiceProvider)ctx.Items[HostServiceProvider]!;
                        await next(ctx);
                    });
                }

                // Switch the request services to the tenant service provider
                if (context.Items.TryGetValue(TenantServiceProvider, out var obj))
                {
                    context.RequestServices = Unsafe.As<TenantContextServiceProvider>(obj!);
                    await requestDelegate(context);
                    return;
                }

                var previousServiceProvider = context.RequestServices;

                context.Items[HostServiceProvider] = previousServiceProvider;

                await using var scope = tenant.Services.CreateAsyncScope();

                var serviceProvider = new TenantContextServiceProvider(
                    scope.ServiceProvider,
                    context.RequestServices,
                    context.RequestServices.GetRequiredService<ILogger<TenantContextServiceProvider>>()
                );

                context.Items[TenantServiceProvider] = serviceProvider;
                context.RequestServices = serviceProvider;

                try
                {
                    await requestDelegate(context);
                }
                finally
                {
                    context.RequestServices = previousServiceProvider;
                }
            };
        });

        async Task<RequestDelegate> GetRequestDelegate(ITenantContext tenant, TenantRequestDelegates requestDelegates, RequestDelegate next)
        {
            var (delegates, semaphore) = requestDelegates;

            await semaphore.WaitAsync();

            try
            {
                if (delegates.TryGetValue(middlewareId, out var requestDelegate))
                {
                    return requestDelegate;
                }

                var tenantBuilder = new TenantApplication(tenant.Services, app.ServerFeatures, tenant.Code, app.ApplicationServices);
                configure(tenantBuilder);

                return delegates.GetOrAdd(middlewareId, tenantBuilder.Build(next));
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}