using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base;

public static class ServiceProviderAccessorExtensions
{
    [CanBeNull]
    public static HttpContext GetHttpContext(this IServiceProviderAccessor serviceProviderAccessor)
    {
        return serviceProviderAccessor.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    }
}