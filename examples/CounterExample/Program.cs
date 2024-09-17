using GerardSmit.AspNetCore.MultiTenant;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMultiTenant(hostBuilder =>
{
    hostBuilder.ResolveTenantByHost(includePort: true);

    hostBuilder.ConfigureTenantServices(tenantServices =>
    {
        tenantServices.AddSingleton<Counter>();
    });
});

var app = builder.Build();

app.UseMultiTenant(tenantBuilder =>
{
    tenantBuilder.Run(async context =>
    {
        if (context.Request.Path != "/")
        {
            // Ignore favicon requests
            context.Response.StatusCode = 404;
            return;
        }

        var tenant = context.GetTenant();
        var counter = context.RequestServices.GetRequiredService<Counter>();

        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync($"Hello from tenant '{tenant.Code}'! Request count: {++counter.Value}");
    });
});

app.Run();

internal class Counter
{
    public int Value { get; set; }
}