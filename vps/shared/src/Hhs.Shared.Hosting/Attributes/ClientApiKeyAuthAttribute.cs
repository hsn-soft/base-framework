using System.Net;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Subscribe;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.Shared.Hosting.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ClientApiKeyAuthAttribute : ActionFilterAttribute
{
    public string KeyLabel { get; set; }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (string.IsNullOrWhiteSpace(KeyLabel))
        {
            throw new ArgumentNullException(nameof(KeyLabel));
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(KeyLabel, out var requestClientApiKey))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest, "API Key is required");
        }

        var serviceProvider = context.HttpContext.RequestServices;

        var config = serviceProvider.GetRequiredService<IConfiguration>();

        string apiKeySettingValue = config.GetValue<string>(KeyLabel);

        if (string.IsNullOrWhiteSpace(apiKeySettingValue))
        {
            throw new ArgumentNullException(nameof(apiKeySettingValue));
        }

        if (!requestClientApiKey.ToString().Equals(apiKeySettingValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new BaseHttpException((int)HttpStatusCode.Unauthorized, "API Key is not authorized!");
        }

        var dataFilter = serviceProvider.GetRequiredService<IDataFilter>();

        using (dataFilter.Disable<IMultiTenant>())
        {
            using (dataFilter.Disable<IScopeSubscription>())
            {
                await next();
            }
        }
    }
}