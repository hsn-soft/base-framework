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
        services.AddScoped<ICurrentClient, CurrentClient>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddSingleton<IStringEncryptionService, StringEncryptionService>();

        services.AddSingleton<ISecurityLogManager, DefaultSecurityLogManager>();
        services.AddScoped<ISecurityLogStore, SimpleSecurityLogStore>();

        return services;
    }
}