using System;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.Tracing;

public class AspNetCoreCorrelationIdProvider : ICorrelationIdProvider, ITransientDependency
{
    public AspNetCoreCorrelationIdProvider(
        IHttpContextAccessor httpContextAccessor,
        IOptions<BaseCorrelationIdOptions> options)
    {
        HttpContextAccessor = httpContextAccessor;
        Options = options.Value;
    }

    protected IHttpContextAccessor HttpContextAccessor { get; }
    protected BaseCorrelationIdOptions Options { get; }

    public virtual string Get()
    {
        if (HttpContextAccessor.HttpContext?.Request?.Headers == null)
        {
            return CreateNewCorrelationId();
        }

        string correlationId = HttpContextAccessor.HttpContext.Request.Headers[Options.HttpHeaderName];

        if (correlationId.IsNullOrEmpty())
        {
            lock (HttpContextAccessor.HttpContext.Request.Headers)
            {
                if (correlationId.IsNullOrEmpty())
                {
                    correlationId = CreateNewCorrelationId();
                    HttpContextAccessor.HttpContext.Request.Headers[Options.HttpHeaderName] = correlationId;
                }
            }
        }

        return correlationId;
    }

    protected virtual string CreateNewCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }
}