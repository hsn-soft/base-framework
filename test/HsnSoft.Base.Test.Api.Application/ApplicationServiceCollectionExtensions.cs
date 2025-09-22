using HsnSoft.Base.Test.Api.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Test.Api.Application;

public static class AppService
{
    public static string AppId { get; set; }
    public static string AppName { get; set; }
}

public static class ApplicationServiceCollectionExtensions
{
    public static void AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IEfCoreUserService, EfCoreUserService>();
        services.AddScoped<IMongoUserService, MongoUserService>();
    }
}