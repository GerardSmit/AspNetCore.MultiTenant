using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GerardSmit.AspNetCore.MultiTenant.Builder;

/// <summary>
/// Defines interface that provides the mechanisms to configure an tenants's request pipeline.
/// </summary>
public interface ITenantApplicationBuilder : IApplicationBuilder, IEndpointRouteBuilder
{
    /// <summary>
    /// The current tenant code that is being built.
    /// </summary>
    string TenantCode { get; }

    /// <summary>
    /// Gets the services of the tenant.
    /// </summary>
    IServiceProvider TenantServices { get; }

    /// <summary>
    /// Gets the services of the host.
    /// </summary>
    IServiceProvider HostServices { get; }

    /// <inheritdoc cref="IApplicationBuilder.Use"/>
    new ITenantApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

    /// <inheritdoc cref="IApplicationBuilder.New"/>
    new ITenantApplicationBuilder New();

    /// <inheritdoc cref="IApplicationBuilder.Use"/>
    IApplicationBuilder IApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate> middleware) => Use(middleware);

    /// <inheritdoc cref="IApplicationBuilder.New"/>
    IApplicationBuilder IApplicationBuilder.New() => New();
}