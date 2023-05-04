using System;
using HsnSoft.Base.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.AspNetCore.WebClientInfo;

public class HttpContextWebClientInfoProvider : IWebClientInfoProvider, ITransientDependency
{
    public HttpContextWebClientInfoProvider(
        ILogger<HttpContextWebClientInfoProvider> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        Logger = logger;
        HttpContextAccessor = httpContextAccessor;
    }

    protected ILogger<HttpContextWebClientInfoProvider> Logger { get; }
    protected IHttpContextAccessor HttpContextAccessor { get; }

    public string BrowserInfo => GetBrowserInfo();

    public string ClientIpAddress => GetClientIpAddress();

    protected virtual string GetBrowserInfo()
    {
        return HttpContextAccessor.HttpContext?.Request?.Headers?["User-Agent"];
    }

    protected virtual string GetClientIpAddress()
    {
        try
        {
            return HttpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, LogLevel.Warning);
            return null;
        }
    }
}