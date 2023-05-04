using HsnSoft.Base.DependencyInjection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.Mvc.AntiForgery;

public class BaseAspNetCoreAntiForgeryManager : IBaseAntiForgeryManager, ITransientDependency
{
    private readonly IAntiforgery _antiforgery;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BaseAspNetCoreAntiForgeryManager(
        IAntiforgery antiforgery,
        IHttpContextAccessor httpContextAccessor,
        IOptions<BaseAntiForgeryOptions> options)
    {
        _antiforgery = antiforgery;
        _httpContextAccessor = httpContextAccessor;
        Options = options.Value;
    }

    protected BaseAntiForgeryOptions Options { get; }

    protected HttpContext HttpContext => _httpContextAccessor.HttpContext;

    public virtual void SetCookie()
    {
        HttpContext.Response.Cookies.Append(
            Options.TokenCookie.Name,
            GenerateToken(),
            Options.TokenCookie.Build(HttpContext)
        );
    }

    public virtual string GenerateToken()
    {
        return _antiforgery.GetAndStoreTokens(_httpContextAccessor.HttpContext).RequestToken;
    }
}