using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.Tracing;

public class BaseCorrelationIdMiddleware : IMiddleware, ITransientDependency
{
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly BaseCorrelationIdOptions _options;

    public BaseCorrelationIdMiddleware(IOptions<BaseCorrelationIdOptions> options,
        ICorrelationIdProvider correlationIdProvider)
    {
        _options = options.Value;
        _correlationIdProvider = correlationIdProvider;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = _correlationIdProvider.Get();

        try
        {
            await next(context);
        }
        finally
        {
            CheckAndSetCorrelationIdOnResponse(context, _options, correlationId);
        }
    }

    protected virtual void CheckAndSetCorrelationIdOnResponse(
        HttpContext httpContext,
        BaseCorrelationIdOptions options,
        string correlationId)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        if (!options.SetResponseHeader)
        {
            return;
        }

        if (httpContext.Response.Headers.ContainsKey(options.HttpHeaderName))
        {
            return;
        }

        httpContext.Response.Headers[options.HttpHeaderName] = correlationId;
    }
}