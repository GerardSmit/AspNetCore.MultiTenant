using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

internal class TenantWebHostEnvironment(IHostEnvironment rootEnvironment, string? webRootPath = null) : IWebHostEnvironment
{
    private string? _applicationName;
    private string? _environmentName;
    private string? _contentRootPath;
    private IFileProvider? _webRootFileProvider;
    private IFileProvider? _contentRootFileProvider;
    private string? _webRootPath = webRootPath;

    public string ApplicationName
    {
        get => _applicationName ?? rootEnvironment.ApplicationName;
        set => _applicationName = value;
    }

    public string EnvironmentName
    {
        get => _environmentName ?? rootEnvironment.EnvironmentName;
        set => _environmentName = value;
    }

    public IFileProvider ContentRootFileProvider
    {
        get => _contentRootFileProvider ??= CreateContentRootFileProvider();
        set => _contentRootFileProvider = value;
    }

    private IFileProvider CreateContentRootFileProvider()
    {
        if (ContentRootPath == rootEnvironment.ContentRootPath)
        {
            return new PhysicalFileProvider(ContentRootPath);
        }

        return new CompositeFileProvider(
            new PhysicalFileProvider(ContentRootPath),
            rootEnvironment.ContentRootFileProvider
        );
    }

    public string ContentRootPath
    {
        get => _contentRootPath ?? rootEnvironment.ContentRootPath;
        set
        {
            _contentRootPath = value;
            _contentRootFileProvider = null;
        }
    }

    public IFileProvider WebRootFileProvider
    {
        get => _webRootFileProvider ??= new PhysicalFileProvider(WebRootPath);
        set => _webRootFileProvider = value;
    }

    public string WebRootPath
    {
        get => _webRootPath ?? (rootEnvironment as IWebHostEnvironment)?.WebRootPath ?? throw new InvalidOperationException("WebRootPath not set.");
        set
        {
            _webRootPath = value;
            _webRootFileProvider = null;
        }
    }
}
