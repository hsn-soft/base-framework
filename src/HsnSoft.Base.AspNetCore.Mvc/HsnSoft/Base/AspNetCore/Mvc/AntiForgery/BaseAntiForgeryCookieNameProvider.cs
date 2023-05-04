using HsnSoft.Base.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore.Mvc.AntiForgery;

public class BaseAntiForgeryCookieNameProvider : ITransientDependency
{
    private readonly BaseAntiForgeryOptions _baseAntiForgeryOptions;
    private readonly IOptionsMonitor<CookieAuthenticationOptions> _namedOptionsAccessor;

    public BaseAntiForgeryCookieNameProvider(
        IOptionsMonitor<CookieAuthenticationOptions> namedOptionsAccessor,
        IOptions<BaseAntiForgeryOptions> baseAntiForgeryOptions)
    {
        _namedOptionsAccessor = namedOptionsAccessor;
        _baseAntiForgeryOptions = baseAntiForgeryOptions.Value;
    }

    public virtual string GetAuthCookieNameOrNull()
    {
        if (_baseAntiForgeryOptions.AuthCookieSchemaName == null)
        {
            return null;
        }

        return _namedOptionsAccessor.Get(_baseAntiForgeryOptions.AuthCookieSchemaName)?.Cookie?.Name;
    }

    public virtual string GetAntiForgeryCookieNameOrNull()
    {
        return _baseAntiForgeryOptions.TokenCookie.Name;
    }
}