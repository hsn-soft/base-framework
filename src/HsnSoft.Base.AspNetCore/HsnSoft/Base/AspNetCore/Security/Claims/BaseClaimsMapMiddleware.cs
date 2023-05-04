using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.Security.Claims;

public class BaseClaimsMapMiddleware : IMiddleware, ITransientDependency
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var currentPrincipalAccessor = context.RequestServices
            .GetRequiredService<ICurrentPrincipalAccessor>();

        var mapOptions = context.RequestServices
            .GetRequiredService<IOptions<BaseClaimsMapOptions>>().Value;

        var mapClaims = currentPrincipalAccessor
            .Principal
            .Claims
            .Where(claim => mapOptions.Maps.Keys.Contains(claim.Type));

        currentPrincipalAccessor
            .Principal
            .AddIdentity(
                new ClaimsIdentity(
                    mapClaims
                        .Select(
                            claim => new Claim(
                                mapOptions.Maps[claim.Type](),
                                claim.Value,
                                claim.ValueType,
                                claim.Issuer
                            )
                        )
                )
            );

        await next(context);
    }
}