using System.Security.Claims;
using System.Threading;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Security.Claims;

public class HttpContextCurrentPrincipalAccessor : CurrentPrincipalAccessorBase, ISingletonDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override ClaimsPrincipal GetClaimsPrincipal()
    {
        return _httpContextAccessor.HttpContext?.User ?? Thread.CurrentPrincipal as ClaimsPrincipal;
    }
}