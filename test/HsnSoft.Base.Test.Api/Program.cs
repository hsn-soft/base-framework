using HsnSoft.Base.Serilog;
using HsnSoft.Base.Test.Api.EfCore.Context;
using HsnSoft.Base.Test.Api.MongoDb.Context;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HsnSoft.Base.Test.Api;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string workspace = typeof(Startup).Namespace;
        ApplicationIdentifier.AppId = Guid.NewGuid().ToString("N");
        ApplicationIdentifier.AppName = workspace?[(workspace.IndexOf('.') + 1)..];

        Log.Logger = SerilogConfigurationHelper.ConfigureConsoleLogger(GetConfiguration());

        try
        {
            Log.Information("Configuring web host ({ApplicationContext})...", ApplicationIdentifier.AppName);
            var host = CreateHostBuilder(args);

            // Seed Data
            using (var scope = host.Services.CreateScope())
            {
                bool isReadyDatabase = false;
                var efCoreDbContext = scope.ServiceProvider.GetRequiredService<AppEfCoreDbContext>();
                try
                {
                    if (efCoreDbContext.Database.CanConnectAsync().GetAwaiter().GetResult())
                    {
                        if ((await efCoreDbContext.Database.GetPendingMigrationsAsync()).Any())
                        {
                            // apply pending migrations
                            await efCoreDbContext.Database.MigrateAsync();
                            Log.Information("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED", "EfCoreSeederService");
                        }
                        else
                        {
                            Log.Information("{WorkerName} | EVERYTHING IS UP TO DATE", "EfCoreSeederService");
                        }
                    }
                    else
                    {
                        // first creation
                        await efCoreDbContext.Database.MigrateAsync();
                        Log.Information("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", "EfCoreSeederService");
                    }

                    isReadyDatabase = true;
                }
                catch (Exception e)
                {
                    Log.Error("{WorkerName} | {OperationStatus} | {Error}", "EfCoreSeederService", "FAIL", e.Message);
                }

                if (isReadyDatabase)
                {
                    if (!efCoreDbContext.Users.Any())
                    {
                        efCoreDbContext.Users.AddRange(SeedData.GenerateUserData(5));
                        await efCoreDbContext.SaveChangesAsync();
                    }
                }

                var mongoDbContext = scope.ServiceProvider.GetRequiredService<AppMongoDbContext>();
                try
                {
                    // Check - Is NormalizedRequest Collection Initialized
                    long estimatedUserDocCount = await mongoDbContext.Users.EstimatedDocumentCountAsync();
                    if (estimatedUserDocCount < 1)
                    {
                        await mongoDbContext.Users.InsertManyAsync(SeedData.GenerateUserData(5));
                    }
                }
                catch (Exception e)
                {
                    Log.Error("{WorkerName} | {OperationStatus} | {Error}", "EfCoreSeederService", "FAIL", e.Message);
                }
            }

            Log.Information("Starting web host ({ApplicationContext})...", ApplicationIdentifier.AppName);
            await host.RunAsync();

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
    }

    private static IWebHost CreateHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .CaptureStartupErrors(false)
            .ConfigureKestrel((context, options) =>
            {
                options.Limits.MaxRequestBufferSize = long.MaxValue;
                options.Limits.MaxRequestBodySize = long.MaxValue;

                var env = context.HostingEnvironment;
                if (!env.IsDevelopment()) return;

                options.ListenAnyIP(6680);
            })
            .ConfigureAppConfiguration(x => x.AddConfiguration(GetConfiguration()))
            .UseStartup<Startup>()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog(Log.Logger);
            })
            .Build();

    private static IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json")
            .AddEnvironmentVariables()
            .Build();
}