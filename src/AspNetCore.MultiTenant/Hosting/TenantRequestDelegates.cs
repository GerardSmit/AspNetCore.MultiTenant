using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace GerardSmit.AspNetCore.MultiTenant.Hosting;

internal sealed class TenantRequestDelegates
{
    public ConcurrentDictionary<Guid, RequestDelegate> Delegates { get; } = new();

    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    public void Deconstruct(out ConcurrentDictionary<Guid, RequestDelegate> delegates, out SemaphoreSlim semaphore)
    {
        delegates = Delegates;
        semaphore = Semaphore;
    }
}
