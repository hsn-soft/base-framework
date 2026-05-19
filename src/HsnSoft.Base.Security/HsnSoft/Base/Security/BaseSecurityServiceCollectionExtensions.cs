using HsnSoft.Base.Security.Encryption;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Security;

public static class BaseSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddBaseSecurityEncryptionCollection(this IServiceCollection services)
    {
        services.AddSingleton<IStringEncryptionService, StringEncryptionService>();

        return services;
    }
}