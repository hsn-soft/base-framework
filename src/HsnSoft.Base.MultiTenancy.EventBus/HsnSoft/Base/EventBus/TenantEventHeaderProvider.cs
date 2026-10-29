using System.Collections.Generic;
using HsnSoft.Base.MultiTenancy;

namespace HsnSoft.Base.EventBus;

/// <summary>Writes the tenant of the caller into the headers of every message the service publishes (the headers of the message that caused it win, so a chain keeps the tenant it started in).</summary>
internal sealed class TenantEventHeaderProvider(ICurrentTenant tenant) : IEventHeaderProvider
{
    public string? GetCorrelationId() => null;

    public void AddHeaders(IDictionary<string, string> headers)
    {
        if (tenant.Id is not { } id)
        {
            return;
        }

        headers[TenantEventHeaders.TenantId] = id.ToString();
        if (tenant.IsSystemTenant)
        {
            headers[TenantEventHeaders.IsSystemTenant] = "true";
        }
    }
}
