using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace HsnSoft.Base.Swashbuckle;

public static class SwaggerConfigurationHelper
{
    public static void Configure(IServiceCollection services, string apiTitle, string apiVersion = "v1", string apiName = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddBaseSwaggerGen(options =>
        {
            options.SwaggerDoc(apiName, new OpenApiInfo { Title = apiTitle, Version = apiVersion });
            options.DocInclusionPredicate((docName, description) => true);
            options.CustomSchemaIds(type => type.FullName);
        });
        services.AddOpenApi();
    }

    public static void ConfigureWithBearer(IServiceCollection services, string tokenDescription,
        string apiTitle, string apiVersion = "v1", string apiName = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddBaseSwaggerGenWithBearer(
            tokenDescription: tokenDescription,
            options =>
            {
                options.SwaggerDoc(apiName, new OpenApiInfo { Title = apiTitle, Version = apiVersion });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
        services.AddOpenApi();
    }
}