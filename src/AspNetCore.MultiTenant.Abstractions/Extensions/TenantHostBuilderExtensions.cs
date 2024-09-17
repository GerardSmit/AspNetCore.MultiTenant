using System.Diagnostics.CodeAnalysis;
using GerardSmit.AspNetCore.MultiTenant.Builder;
using GerardSmit.AspNetCore.MultiTenant.Hosting;
using GerardSmit.AspNetCore.MultiTenant.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GerardSmit.AspNetCore.MultiTenant;

public static class TenantHostBuilderExtensions
{
    public static TenantHostBuilder AddTenantInitializer(this TenantHostBuilder builder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        builder.HostServices.AddSingleton(typeof(ITenantInitializer), type);
        return builder;
    }

    public static TenantHostBuilder AddTenantInitializer<TInitializer>(this TenantHostBuilder builder)
        where TInitializer : class, ITenantInitializer
    {
        return builder.AddTenantInitializer(typeof(TInitializer));
    }

    public static TenantHostBuilder ConfigureOptions(this TenantHostBuilder builder, Action<TenantOptions> configure)
    {
        builder.HostServices.Configure(configure);
        return builder;
    }

    public static TenantHostBuilder ConfigureTenant(this TenantHostBuilder builder, Action<ITenantBuilder> configureServices)
    {
        builder.HostServices.AddSingleton<ITenantInitializer>(new ActionTenantInitializer(tenantBuilder =>
        {
            configureServices(tenantBuilder);
            return ValueTask.CompletedTask;
        }));

        return builder;
    }

    public static TenantHostBuilder ConfigureTenantServices(this TenantHostBuilder builder, Action<IServiceCollection> configureServices)
    {
        builder.HostServices.AddSingleton<ITenantInitializer>(new ActionTenantInitializer(b =>
        {
            configureServices(b.TenantServices);
            return ValueTask.CompletedTask;
        }));

        return builder;
    }

    public static TenantHostBuilder ConfigureTenant(this TenantHostBuilder builder, Func<ITenantBuilder, ValueTask> configureServices)
    {
        builder.HostServices.AddSingleton<ITenantInitializer>(new ActionTenantInitializer(configureServices));
        return builder;
    }

    private class ActionTenantInitializer(Func<ITenantBuilder, ValueTask> configure) : ITenantInitializer
    {
        public ValueTask InitializeAsync(ITenantBuilder builder)
        {
            return configure(builder);
        }
    }

    public static TenantHostBuilder RedirectSingletonServiceToHost(
        this TenantHostBuilder builder,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        builder.HostServices.AddSingleton<ITenantInitializer>(new RedirectHostServiceInitializer(type, ServiceLifetime.Singleton));
        return builder;
    }

    public static TenantHostBuilder RedirectSingletonServiceToHost<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this TenantHostBuilder builder)
        where TService : class
    {
        return builder.RedirectSingletonServiceToHost(typeof(TService));
    }

    private class RedirectHostServiceInitializer(Type type, ServiceLifetime lifetime) : ITenantInitializer
    {
        public ValueTask InitializeAsync(ITenantBuilder builder)
        {
            builder.TenantServices.Add(new ServiceDescriptor(type, _ => builder.HostServices.GetRequiredService(type), lifetime));
            return ValueTask.CompletedTask;
        }
    }

    public static TenantHostBuilder RedirectScopedServiceToHost(
        this TenantHostBuilder builder,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        builder.HostServices.AddSingleton<ITenantInitializer>(new RedirectHostServiceInitializer(type, ServiceLifetime.Scoped));
        builder.ConfigureOptions(options => options.RedirectScopeTypes.Add(type));
        return builder;
    }

    public static TenantHostBuilder RedirectScopedServiceToHost<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this TenantHostBuilder builder)
        where TService : class
    {
        return builder.RedirectScopedServiceToHost(typeof(TService));
    }
}
