using System.Text.Json.Serialization;
using HsnSoft.Base.Test.Api.Application;
using HsnSoft.Base.Test.Api.EfCore;
using HsnSoft.Base.Test.Api.MongoDb;
using Microsoft.OpenApi.Models;
using Serilog;

namespace HsnSoft.Base.Test.Api;

public sealed class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    private IConfiguration Configuration { get; } = configuration;
    private IWebHostEnvironment WebHostEnvironment { get; } = environment;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{AppService.AppName} API", Version = "v1" }); });

        services.AddServiceApplicationConfiguration(Configuration);
        services.AddServiceEfCoreDatabaseConfiguration(Configuration);
        services.AddServiceMongoDbDatabaseConfiguration(Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{AppService.AppName} API"));
        }

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());

        hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
    }


    private static void OnShutdown() => Log.Information("Stopping web host ({ApplicationContext})...", AppService.AppName);
}