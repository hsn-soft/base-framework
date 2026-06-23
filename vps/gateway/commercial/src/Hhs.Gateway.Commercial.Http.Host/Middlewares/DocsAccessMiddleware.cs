using System.Security.Claims;
using Hhs.Gateway.Commercial.Helpers;
using Hhs.Gateway.Commercial.Options;
using HsnSoft.Base.AspNetCore.Responses;
using Microsoft.Extensions.Options;

namespace Hhs.Gateway.Commercial.Middlewares;

public sealed class DocsAccessMiddleware : IMiddleware
{
    private readonly GatewayPolicyOptions _options;
    private readonly IApiResponseWriter _writer;

    public DocsAccessMiddleware(
        IOptions<GatewayPolicyOptions> options,
        IApiResponseWriter writer)
    {
        _options = options.Value;
        _writer = writer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!GatewayRouteClassifier.MatchesAnyPrefix(path, _options.Routes.DocsPrefixes))
        {
            await next(context);
            return;
        }

        if (_options.DocsAllowInternalAnonymousAccess &&
            IpMatcher.IsMatch(context.Connection.RemoteIpAddress, _options.DocsInternalCidrs))
        {
            await next(context);
            return;
        }

        bool isAuthenticated = context.User?.Identity?.IsAuthenticated == true;

        if (isAuthenticated && HasAllowedRole(context.User!, _options.DocsAllowedRoles))
        {
            await next(context);
            return;
        }

        if (_options.DocsRequireAuthenticatedUser)
        {
            await _writer.WriteErrorAsync(
                context,
                StatusCodes.Status404NotFound,
                ["Not Found"],
                "docs_not_found");
            return;
        }

        await next(context);
    }

    private static bool HasAllowedRole(ClaimsPrincipal principal, IEnumerable<string> roles)
        => roles.Any(principal.IsInRole);
}