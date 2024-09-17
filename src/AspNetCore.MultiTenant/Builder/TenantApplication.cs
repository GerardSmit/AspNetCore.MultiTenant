using System.Diagnostics;
using GerardSmit.AspNetCore.MultiTenant.Collections;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GerardSmit.AspNetCore.MultiTenant.Builder;

internal class TenantApplication : ITenantApplicationBuilder
{
    private const string GlobalEndpointRouteBuilderKey = "__GlobalEndpointRouteBuilder";
    private const string EndpointRouteBuilderKey = "__EndpointRouteBuilder";
    private const string AuthenticationMiddlewareSetKey = "__AuthenticationMiddlewareSet";
    private const string AuthorizationMiddlewareSetKey = "__AuthorizationMiddlewareSet";
    private const string UseRoutingKey = "__UseRouting";

    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = [];
    private readonly List<EndpointDataSource> _dataSources = [];

    public TenantApplication(IServiceProvider tenantServiceProvider, IFeatureCollection serverFeatures, string tenant, IServiceProvider hostServices)
    {
        TenantCode = tenant;
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        TenantServices = tenantServiceProvider;
        HostServices = hostServices;
        ServerFeatures = serverFeatures;

        Properties[GlobalEndpointRouteBuilderKey] = this;
    }

    public TenantApplication(TenantApplication builder)
    {
        TenantCode = builder.TenantCode;
        Properties = new CopyOnWriteDictionary<string, object?>(builder.Properties, StringComparer.Ordinal);
        TenantServices = builder.TenantServices;
        HostServices = builder.HostServices;
        ServerFeatures = builder.ServerFeatures;
    }

    public string TenantCode { get; }

    public ITenantApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    public ITenantApplicationBuilder New()
    {
        return new TenantApplication(this);
    }

    public RequestDelegate Build(RequestDelegate next)
    {
        var app = new TenantApplication(this);

        // UseRouting called before WebApplication such as in a StartupFilter
        // lets remove the property and reset it at the end so we don't mess with the routes in the filter
        app.Properties.Remove(EndpointRouteBuilderKey, out var priorRouteBuilder);

        // Wrap the entire destination pipeline in UseRouting() and UseEndpoints(), essentially:
        // destination.UseRouting()
        // destination.Run(source)
        // destination.UseEndpoints()

        // Set the route builder so that UseRouting will use the WebApplication as the IEndpointRouteBuilder for route matching
        app.Properties[GlobalEndpointRouteBuilderKey] = this;

        // Only call UseRouting() if there are endpoints configured and UseRouting() wasn't called on the global route builder already
        if (_dataSources.Count > 0)
        {
            // If this is set, someone called UseRouting() when a global route builder was already set
            if (!Properties.TryGetValue(EndpointRouteBuilderKey, out var localRouteBuilder))
            {
                app.UseRouting();
                // Middleware the needs to re-route will use this property to call UseRouting()
                Properties[UseRoutingKey] = app.Properties[UseRoutingKey];
            }
            else
            {
                // UseEndpoints will be looking for the RouteBuilder so make sure it's set
                app.Properties[EndpointRouteBuilderKey] = localRouteBuilder;
            }
        }

        // Process authorization and authentication middlewares independently to avoid
        // registering middlewares for services that do not exist
        var serviceProviderIsService = TenantServices.GetService<IServiceProviderIsService>();
        if (serviceProviderIsService?.IsService(typeof(IAuthenticationSchemeProvider)) is true)
        {
            // Don't add more than one instance of the middleware
            if (!Properties.ContainsKey(AuthenticationMiddlewareSetKey))
            {
                // The Use invocations will set the property on the outer pipeline,
                // but we want to set it on the inner pipeline as well.
                Properties[AuthenticationMiddlewareSetKey] = true;
                app.UseAuthentication();
            }
        }

        if (serviceProviderIsService?.IsService(typeof(IAuthorizationHandlerProvider)) is true)
        {
            if (!Properties.ContainsKey(AuthorizationMiddlewareSetKey))
            {
                Properties[AuthorizationMiddlewareSetKey] = true;
                app.UseAuthorization();
            }
        }

        app.Use(BuildInternal);

        if (_dataSources.Count > 0)
        {
            // We don't know if user code called UseEndpoints(), so we will call it just in case, UseEndpoints() will ignore duplicate DataSources
            app.UseEndpoints(_ => { });
        }

        // Remove the route builder to clean up the properties, we're done adding routes to the pipeline
        app.Properties.Remove(GlobalEndpointRouteBuilderKey);

        // reset route builder if it existed, this is needed for StartupFilters
        if (priorRouteBuilder is not null)
        {
            app.Properties[EndpointRouteBuilderKey] = priorRouteBuilder;
        }

        return app.BuildInternal(next);
    }

    private RequestDelegate BuildInternal(RequestDelegate next)
    {
        RequestDelegate app = next;

        for (var c = _components.Count - 1; c >= 0; c--)
        {
            app = _components[c](app);
        }

        return app;
    }

    public IServiceProvider TenantServices { get; set; }

    public IServiceProvider HostServices { get; }

    public IFeatureCollection ServerFeatures { get; }

    public IDictionary<string, object?> Properties { get; }

    IServiceProvider IApplicationBuilder.ApplicationServices
    {
        get => TenantServices;
        set => TenantServices = value;
    }

    RequestDelegate IApplicationBuilder.Build() => throw new NotSupportedException();
    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => New();
    IServiceProvider IEndpointRouteBuilder.ServiceProvider => TenantServices;
    ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => _dataSources;
}