using System.Text.Json.Serialization;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Test.Api.Application;
using HsnSoft.Base.Test.Api.EfCore;
using HsnSoft.Base.Test.Api.MongoDb;
using HsnSoft.Base.Tracing;
using Microsoft.OpenApi.Models;
using Serilog;

namespace HsnSoft.Base.Test.Api;

public sealed class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    private IConfiguration Configuration { get; } = configuration;
    private IWebHostEnvironment WebHostEnvironment { get; } = environment;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddBaseAspNetCoreContextCollection();
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
        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{ApplicationIdentifier.AppName} API", Version = "v1" }); });

        services.AddServiceApplicationConfiguration(Configuration);
        services.AddServiceEfCoreDatabaseConfiguration(Configuration);
        services.AddServiceMongoDbDatabaseConfiguration(Configuration);

        // Workers
        // services.AddHostedService<EfCoreWriterWorker>();
        // services.AddHostedService<EfCoreCleanerWorker>();

        // services.AddHostedService<MongoWriterWorker>();
        // services.AddHostedService<MongoCleanerWorker>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationIdentifier.AppName} API"));
        }

        app.UseRouting();
        app.UseMiddleware<FakeUserMiddleware>();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());

        hostApplicationLifetime.ApplicationStopping.Register(OnShutdown);
    }


    private static void OnShutdown() => Log.Information("Stopping web host ({ApplicationContext})...", ApplicationIdentifier.AppName);
}