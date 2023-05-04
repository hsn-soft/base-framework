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

        services.AddTransient<IStringEncryptionService, StringEncryptionService>();

        services.AddTransient<ISecurityLogManager, DefaultSecurityLogManager>();
        services.AddTransient<ISecurityLogStore, SimpleSecurityLogStore>();

        return services;
    }
}