using System;
using System.Net.Http.Headers;
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
            }

            ctx.Items[CorrelationIdKey] = correlationId.ToString();
            await next();
        });

    public static string? GetCorrelationId(this HttpContext context)
        => context.Items.TryGetValue(CorrelationIdKey, out var correlationId) ? correlationId as string : null;

    public static void AddCorrelationId(this HttpRequestHeaders headers, string correlationId)
        => headers.TryAddWithoutValidation(CorrelationIdKey, correlationId);
}