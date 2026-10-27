using System;
using System.Collections.Generic;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EventBus;

/// <summary>The handler of a message runs in the tenant of the message (<see cref="ICurrentTenant.Change"/>), so the filters and the tenant of a new entity work as in a request; the logs of the handling carry the tenant.</summary>
internal sealed class TenantEventHandlerScope(ICurrentTenant tenant, ILogger<TenantEventHandlerScope> logger) : IEventHandlerScope
{
    public IDisposable Begin(ParentMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!Guid.TryParse(envelope.GetHeader(TenantEventHeaders.TenantId), out Guid id))
        {
            return NoScope.Instance;
        }

        bool isSystem = string.Equals(envelope.GetHeader(TenantEventHeaders.IsSystemTenant), "true", StringComparison.OrdinalIgnoreCase);
        return new Scope(tenant.Change(id, isSystem), logger.BeginScope(new Dictionary<string, object?> { ["TenantId"] = id }));
    }

    private sealed class Scope(IDisposable tenant, IDisposable? log) : IDisposable
    {
        public void Dispose()
        {
            log?.Dispose();
            tenant.Dispose();
        }
    }

    private sealed class NoScope : IDisposable
    {
        public static readonly NoScope Instance = new();

        public void Dispose()
        {
        }
    }
}
