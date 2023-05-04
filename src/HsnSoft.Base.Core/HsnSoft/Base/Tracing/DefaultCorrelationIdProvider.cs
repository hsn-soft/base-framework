using System;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Tracing;

public class DefaultCorrelationIdProvider : ICorrelationIdProvider, ISingletonDependency
{
    public string Get()
    {
        return CreateNewCorrelationId();
    }

    protected virtual string CreateNewCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }
}