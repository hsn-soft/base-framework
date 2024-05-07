using HsnSoft.Base.Clients;
using HsnSoft.Base.Security.Encryption;
using HsnSoft.Base.SecurityLog;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Security;

public static class BaseSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddBaseSecurityServiceCollection(this IServiceCollection services)
    {
        services.AddTransient<ICurrentClient, CurrentClient>();
        services.AddTransient<ICurrentUser, CurrentUser>();

        return services;
    }

    public static IServiceCollection AddBaseSecurityEncryptionCollection(this IServiceCollection services)
    {
        services.AddSingleton<IStringEncryptionService, StringEncryptionService>();

        return services;
    }

    public static IServiceCollection AddBaseSecurityLogCollection(this IServiceCollection services)
    {
        services.AddScoped<ISecurityLogStore, SimpleSecurityLogStore>();
        services.AddSingleton<ISecurityLogManager, DefaultSecurityLogManager>();


        return services;
    }
}