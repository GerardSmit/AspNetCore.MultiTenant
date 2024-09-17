namespace GerardSmit.AspNetCore.MultiTenant;

public sealed class TenantContext(string code) : ITenantContext
{
    public string Code { get; } = code;

    public IServiceProvider Services { get; set; } = null!;

    public TenantState State { get; set; } = TenantState.Starting;

    public DateTimeOffset LastAccessed { get; set; } = DateTimeOffset.UtcNow;
}
