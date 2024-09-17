# Tenant
GerardSmit.AspNetCore.MultiTenant is a simple library that allows you to easily implement multi-tenancy in your ASP.NET Core application.

The way this library resolves multi-tenancy is to create a new service provider for each tenant. The main reason for this, is that seperating data between tenants is a lot easier; you don't have to worry about filtering data based on the tenant.

## Example


```cs
using GerardSmit.AspNetCore.MultiTenant;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMultiTenant(hostBuilder =>
{
    // Resolve the tenant by the host name of the request.
    hostBuilder.ResolveTenantByHost(includePort: true);

    hostBuilder.ConfigureTenantServices(tenantServices =>
    {
        // Tenant specific services
        tenantServices.AddSingleton<Counter>();
    });
});

var app = builder.Build();

app.UseMultiTenant(tenantBuilder =>
{
    // Tenant specific middleware with tenant specific services
    tenantBuilder.Run(async context =>
    {
        var tenant = context.GetTenant();
        var counter = context.RequestServices.GetRequiredService<Counter>();

        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync($"Hello from tenant '{tenant.Code}'! Request count: {counter.Value++}");
    });
});

app.Run();

internal class Counter
{
    public int Value { get; set; }
}
```
Runnable project: [examples/CounterExample](examples/CounterExample).

## API usage
> [!IMPORTANT]  
> In this library, we refer "host services" to the default service provider of ASP.NET Core, and "tenant services" to the service provider of a tenant.

### Add the host services
To add the host services, you can use the `AddMultiTenant` method:

```cs
builder.Services.AddMultiTenant();
```

### Resolving the tenant code
You can use the default resolver that uses the host name to resolve the tenant code:

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    // Adds a 'ITenantResolver' that resolves the tenant by the host name of the request.
    // By default, the port is included in the host name. This can be disabled by setting 'includePort' to false.
    hostBuilder.ResolveTenantByHost(includePort: true);
});
```

Or you can create your own resolver by implementing the `ITenantResolver` interface:

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    // Adds a 'ITenantResolver' that resolves the tenant by the host name of the request.
    hostBuilder.HostServices.AddSingleton<ITenantResolver, CustomTenantResolver>();
});

public class CustomTenantResolver : ITenantResolver
{
    public ValueTask<string?> ResolveAsync(HttpContext context)
    {
        return new ValueTask<string?>("tenant-code");
    }
}
```



### Registering tenant specific services
To register tenant specific services, you can use the `ConfigureTenantServices` in the `AddMultiTenant` method:

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    hostBuilder.ConfigureTenantServices(tenantServices =>
    {
        // Add the memory cache to the tenant services.
        // Every tenant will have its own memory cache.
        tenantServices.AddMemoryCache();
    });
});
```

If you want to redirect a type to the host service provider, you can use the `RedirectSingletonServiceToHost` or `RedirectScopedServiceToHost` methods:

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    // Redirects the 'IMemoryCache' to the host service provider.
    // Every tenant will use the same memory cache.
    // Be careful with this, because it can lead to data leakage between tenants!
    hostBuilder.RedirectSingletonServiceToHost(typeof(IMemoryCache));
});
```

If you need to configure the tenant services based on the host services, you can use the `ConfigureTenant` method (async is optional):

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    hostBuilder.ConfigureTenant(async tenantBuilder =>
    {
        var hostService = tenantBuilder.HostServices.GetRequiredService<IHostService>();

        if (await hostService.CanUseCacheAsync(tenantBuilder.TenantCode))
        {
            tenantBuilder.TenantServices.AddMemoryCache();
        }
    });
});
```

### Registering tenant specific middleware
To add tenant specific middleware, you can use the `UseMultiTenant` method. Every middleware that is added with this method will only be executed for the current tenant:

```cs
app.Use((context, next) =>
{
    // The "context.RequestServices" is the scoped service provider of the host.
    // No tenant specific services are available here.
    return next(context);
});

app.UseMultiTenant(tenantBuilder =>
{
    tenantBuilder.Use((context, next) =>
    {
        // The "context.RequestServices" is the scoped service provider of the tenant.
        return next(context);
    });
});

app.Use((context, next) =>
{
    // After the tenant middleware, the "context.RequestServices" is the scoped service provider of the host again.
    return next(context);
});
```

The scoped tenant service provider is created once per request. This means chaining tenant middleware will resolve the same scoped services:

```cs
app.UseMultiTenant(tenantBuilder =>
{
    tenantBuilder.Use((context, next) =>
    {
        var scopedService = context.RequestServices.GetRequiredService<IScopedService>();

        return next(context);
    });
});

app.Use((context, next) =>
{
    // Some other ASP.NET middleware that's running on the host.
    return next(context);
});

app.UseMultiTenant(tenantBuilder =>
{
    tenantBuilder.Use((context, next) =>
    {
        var scopedService = context.RequestServices.GetRequiredService<IScopedService>();
        // 'scopedService' is the same instance as the previous tenant middleware.
        
        return next(context);
    });
});
```

### Logging
If you want to redirect the logging from the tenant to the host, you can use the `RedirectLoggingToHost` method:

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    hostBuilder.RedirectLoggingToHost();
});
```

By default it'll change the category name of the logger to `[tenant-code] category-name`. You can change this adding a lambda to the `RedirectLoggingToHost` method:

```cs
builder.Services.AddMultiTenant(hostBuilder =>
{
    hostBuilder.RedirectLoggingToHost((tenantCode, categoryName) => $"{tenantCode} - {categoryName}");
});
```

### Resolve host services in a tenant
If you want to access the root provider in a tenant, you can resolve `IHostContext`:

```cs
class TenantService
{
    public TenantService(IHostContext hostContext)
    {
        var hostService = hostContext.HostServices.GetRequiredService<IMemoryCache>();
    }
}
```

### Resolve the current tenant
If you want to access the current tenant code in a service, you can resolve `ITenantContext`:

```cs
class TenantService
{
    public TenantService(ITenantContext tenantContext)
    {
        var code = tenantContext.Code;
    }
}
```

### Manage tenants in the hosting layer
To get or stop a tenant in the hosting layer, you can use `ITenantService`:

```cs
var tenantService = app.Services.GetRequiredService<ITenantService>();

// Gets or starts a tenant with the code "tenant-code".
var tenant = await tenantService.GetOrStartTenantAsync("tenant-code");

// Stops a tenant with the code "tenant-code".
await tanantService.StopAsync("tenant-code");

// Gets all the active tenants.
var tenants = tenantService.ActiveTenants;
```