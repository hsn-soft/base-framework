using System;
using System.Net.Http.Headers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Tracing;

public static class CorrelationExtensions
{
    private const string CorrelationIdKey = "X-Correlation-Id";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Headers.TryGetValue(CorrelationIdKey, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
                ctx.Request.Headers[CorrelationIdKey] = correlationId.ToString();
            }

            await next();
        });

    [CanBeNull]
    public static string GetCorrelationId(this HttpContext context)
        => context.Request.Headers.TryGetValue(CorrelationIdKey, out var correlationId) ? correlationId.ToString() : null;

    public static void AddCorrelationId(this HttpRequestHeaders headers, string correlationId)
        => headers.TryAddWithoutValidation(CorrelationIdKey, correlationId);
}