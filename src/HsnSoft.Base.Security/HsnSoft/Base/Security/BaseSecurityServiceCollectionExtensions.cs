using HsnSoft.Base.Security.Encryption;
using HsnSoft.Base.SecurityLog;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Security;

public static class BaseSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddBaseSecurityEncryptionCollection(this IServiceCollection services)
    {
        services.AddSingleton<IStringEncryptionService, StringEncryptionService>();

        return services;
    }

    public static IServiceCollection AddBaseSecurityLogCollection(this IServiceCollection services)
    {
        services.AddTransient<ISecurityLogStore, SimpleSecurityLogStore>();
        services.AddSingleton<ISecurityLogManager, DefaultSecurityLogManager>();


        return services;
    }
}