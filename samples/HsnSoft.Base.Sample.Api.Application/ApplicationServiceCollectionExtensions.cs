using HsnSoft.Base.Sample.Api.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Sample.Api.Application;

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