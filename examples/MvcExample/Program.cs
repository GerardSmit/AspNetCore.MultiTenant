using GerardSmit.AspNetCore.MultiTenant;
using GerardSmit.AspNetCore.MultiTenant.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMultiTenant(hostBuilder =>
{
    hostBuilder.RedirectLoggingToHost();
    hostBuilder.ResolveTenantByHost(includePort: true);

    hostBuilder.ConfigureTenantServices(tenantServices =>
    {
        tenantServices.AddSingleton<Counter>();

        tenantServices.AddControllers();
        tenantServices.AddEndpointsApiExplorer();
        tenantServices.AddSwaggerGen();
    });
});

var app = builder.Build();

app.UseMultiTenant(tenantBuilder =>
{
    if (app.Environment.IsDevelopment())
    {
        tenantBuilder.UseSwagger();
        tenantBuilder.UseSwaggerUI();
    }

    tenantBuilder.MapGet("/echo", ([FromQuery] string message) => Results.Ok(message))
        .WithName("Echo")
        .WithOpenApi();

    tenantBuilder.MapGet("/counter", ([FromServices] Counter counter) => Results.Ok(new
        {
            Counter = counter.Value++
        }))
        .WithName("Counter")
        .WithOpenApi();
});

app.MapGet("/tenants", ([FromServices] ITenantService tenantService) => Results.Ok(tenantService.ActiveTenants.Select(t => t.Code)))
    .WithName("Tenants")
    .WithOpenApi();

app.Run(async c =>
{
    c.Response.StatusCode = 404;
    c.Response.ContentType = "text/plain";
    await c.Response.WriteAsync("Not Found");
});

app.Run();

internal class Counter
{
    public int Value { get; set; }
}
