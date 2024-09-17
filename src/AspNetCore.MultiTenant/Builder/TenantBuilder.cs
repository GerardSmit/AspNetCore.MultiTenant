using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GerardSmit.AspNetCore.MultiTenant.Builder;

public sealed class TenantBuilder(IServiceCollection services, string code, IServiceProvider hostServices) : ITenantBuilder
{
    public Dictionary<object, object?> Items { get; } = new();

    public IServiceProvider HostServices { get; } = hostServices;

    public IServiceCollection TenantServices { get; set; } = services;

    public string TenantCode { get; } = code;
}