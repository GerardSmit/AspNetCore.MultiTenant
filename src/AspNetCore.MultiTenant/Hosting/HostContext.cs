namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

internal class HostContext(IServiceProvider provider) : IHostContext
{
    public IServiceProvider HostServices { get; } = provider;
}
