using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

internal class TenantHostEnvironment(IHostContext hostContext) : IHostEnvironment
{
    private static readonly NullFileProvider NullFileProvider = new();
    private readonly IHostEnvironment? _parent = hostContext.HostServices.GetService<IHostEnvironment>();

    private string? _contentRootPath;
    private IFileProvider? _contentRootFileProvider;
    private string? _environmentName;
    private string? _applicationName;

    public string EnvironmentName
    {
        get => _environmentName ?? _parent?.EnvironmentName ?? "Production";
        set => _environmentName = value;
    }

    public string ApplicationName
    {
        get => _applicationName ?? _parent?.ApplicationName ?? "";
        set => _applicationName = value;
    }

    public string ContentRootPath
    {
        get => _contentRootPath ?? _parent?.ContentRootPath ?? Directory.GetCurrentDirectory();
        set
        {
            _contentRootPath = value;
            _contentRootFileProvider = null;
        }
    }

    public IFileProvider ContentRootFileProvider
    {
        get => _contentRootFileProvider ?? _parent?.ContentRootFileProvider ?? NullFileProvider;
        set => _contentRootFileProvider = value;
    }
}
