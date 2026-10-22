using HsnSoft.Base.Sample.Api;
using HsnSoft.Base.Sample.Api.Application;
using HsnSoft.Base.Sample.Api.EfCore;
using HsnSoft.Base.Sample.Api.Hosting.Extensions;
using HsnSoft.Base.Sample.Api.MongoDb;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Tracing;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// App info
string workspace = typeof(FakeUserMiddleware).Namespace;
ApplicationIdentifier.AppId = Guid.CreateVersion7().ToString("N");
ApplicationIdentifier.AppName = workspace?[(workspace.IndexOf('.') + 1)..];

// Serilog
Log.Logger = SerilogConfigurationHelper.ConfigureConsoleLogger(builder.Configuration, "Host");
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

// Kestrel
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Limits.MaxRequestBufferSize = long.MaxValue;
    options.Limits.MaxRequestBodySize = long.MaxValue;

    if (!context.HostingEnvironment.IsDevelopment()) return;

    options.ListenAnyIP(6680);
});

// =======================
// ConfigureServices
// =======================
builder.Services.AddSampleMicroserviceHosting(typeof(Program))
    .AddServiceApplicationConfiguration(builder.Configuration);
builder.Services.AddServiceEfCoreDatabaseConfiguration(builder.Configuration);
builder.Services.AddServiceMongoDbDatabaseConfiguration(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = $"{ApplicationIdentifier.AppName} API", Version = "v1" }));

// =======================
// Build
// =======================
try
{
    Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);

    var app = builder.Build();

    if (builder.Configuration.GetValue("Database:AutoInitialize", true))
    {
        await DatabaseInitializer.InitializeAsync(app.Services);
    }

    // =======================
    // Configure (Middleware)
    // =======================
    app.UseCustomExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationIdentifier.AppName} API"));
    }

    app.UseRouting();
    app.UseMiddleware<FakeUserMiddleware>();
    app.UseAuthorization();
    app.MapControllers();

    app.Lifetime.ApplicationStopping.Register(() => Log.Information("Stopping web host ({ApplicationContext})...", ApplicationIdentifier.AppName));

    Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);
    await app.RunAsync();

    Log.Information("Program terminated successfully ({ApplicationContext}).", ApplicationIdentifier.AppName);
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({ApplicationContext})!", ApplicationIdentifier.AppName);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Entry point marker for WebApplicationFactory<Program> in the integration tests.
public partial class Program;
