using System.Text.Json;
using Hhs.Scheduler.ApiTrigger.Host.Models;
using Hhs.Scheduler.ApiTrigger.Host.Services;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Serilog.Loggers;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Quartz;
using Serilog;

// ReSharper disable AccessToDisposedClosure

namespace Hhs.Scheduler.ApiTrigger.Host;

public static class Program
{
    private static bool s_isCancelled;
    private static CancellationTokenSource s_cts;

    public static async Task<int> Main(string[] args)
    {
        // App info
        AppInfoResolver.Resolve(typeof(EventHandlersAssemblyMarker));

        var configuration = GetConfiguration();
        // Serilog
        Log.Logger = SerilogConfigurationHelper.ConfigureConsoleLogger(configuration, "Host");

        s_cts = new CancellationTokenSource();
        try
        {
            Log.Information("Configuring scheduler app ({AppName})...", ApplicationIdentifier.AppName);
            var mainHost = CreateHostBuilder(args);

            // Initialize Scheduler
            await SetScheduleEndpointTriggers(
                mainHost.Services.GetRequiredService<ISchedulerFactory>(),
                await GetServiceScheduledEndpoints(configuration)
            );

            Log.Information("Starting scheduler app ({AppName})...", ApplicationIdentifier.AppName);
            // don't use await, you must not block the thread
            _ = mainHost.RunAsync(s_cts.Token);

            await Task.Delay(500, s_cts.Token);

            Log.Information("Configuring Healthcheck API");
            var healthCheckApp = BuildHealthCheckApp();
            Log.Information("Starting Healthcheck API");
            var healthcheckApiTask = healthCheckApp.RunAsync(s_cts.Token);
            Log.Information("Healthcheck API started.");

            // Shutdown handling
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if (s_isCancelled) return;
                s_isCancelled = true;
                try { s_cts?.Cancel(); }
                catch
                {
                    // ignored
                }
            };
            Console.CancelKeyPress += (_, e) =>
            {
                if (!s_isCancelled)
                {
                    s_isCancelled = true;
                    try { s_cts?.Cancel(); }
                    catch
                    {
                        // ignored
                    }
                }

                e.Cancel = true;
            };

            // Safely Shutdown
            await WaitShutdownSequence(mainHost, healthCheckApp, healthcheckApiTask);

            Log.Information("Terminated scheduler app ({AppName})...", ApplicationIdentifier.AppName);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Program terminated unexpectedly ({AppName})!", ApplicationIdentifier.AppName);
            return 1;
        }
        finally
        {
            s_cts.Dispose();
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task WaitShutdownSequence(IHost mainHost, WebApplication healthCheckApp, Task healthcheckApiTask)
    {
        // Wait for the healthcheck task while the main app is up
        if (healthcheckApiTask != null)
        {
            Log.Information("Waiting for shutdown signal. (Ctrl+C or SIGTERM)");
            await Task.WhenAny(healthcheckApiTask, mainHost.WaitForShutdownAsync());
        }

        // Shutdown signal has come:
        // 1. Shutdown Healthcheck API
        if (healthCheckApp != null)
        {
            Log.Information("Stopping Healthcheck API...");
            await healthCheckApp.StopAsync(TimeSpan.FromSeconds(5));
        }

        if (healthcheckApiTask != null)
        {
            try { await healthcheckApiTask; }
            catch
            {
                // ignored
            }

            Log.Information("Healthcheck API stopped.");
        }

        // 2. Shutdown Main App
        if (mainHost != null)
        {
            Log.Information("Stopping scheduler app ({AppName})...", ApplicationIdentifier.AppName);
            await mainHost.StopAsync(TimeSpan.FromSeconds(10));
        }
    }

    private static WebApplication BuildHealthCheckApp()
    {
        var builder = WebApplication.CreateBuilder([]);
        builder.WebHost
            .CaptureStartupErrors(false)
            .ConfigureKestrel(serverOptions =>
            {
                string envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                serverOptions.ListenAnyIP((envName ?? "").ToLower().Equals("development") ? 7070 : 8080);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog(Log.Logger);
            });
        var app = builder.Build();
        app.MapGet("/health", () => Results.Ok("healthy"));
        return app;
    }

    private static IHost CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(x => x.AddConfiguration(GetConfiguration()))
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddSingleton<ILogMasker, LogMasker>();

                services.AddSingleton<IAppConsoleLogger, AppLogger>();
                services.AddSingleton<IFrameworkLogger, FrameworkLogger>();

                services.AddQuartz(_ =>
                {
                    // quartz configs
                });
                services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

                services.AddHttpClient();

                services.AddHealthChecks();
            })
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

    private static async Task<List<ServiceScheduledEndpoints>> GetServiceScheduledEndpoints(IConfiguration configuration)
    {
        var serviceScheduledEndpoints = new List<ServiceScheduledEndpoints>();

        string defaultEndPointKey = configuration.GetValue<string>("DefaultEndPointKey") ?? string.Empty;
        var workerServices = configuration.GetSection("WorkerServices");
        if (!workerServices.Exists()) return serviceScheduledEndpoints;

        var workerServiceItems = workerServices.GetChildren().ToList();
        if (workerServiceItems is not { Count: > 0 }) return serviceScheduledEndpoints;

        var endpoints = JsonSerializer.Deserialize<List<SchedulerEndpointModel>>(await File.ReadAllTextAsync("scheduler.json"));
        if (endpoints is not { Count: > 0 }) return serviceScheduledEndpoints;

        foreach (var workerServiceItem in workerServiceItems)
        {
            if (string.IsNullOrWhiteSpace(workerServiceItem.Key))
            {
                continue;
            }

            var keyTemplate = workerServiceItem.Get<WorkerServiceItemTemplate>();
            if (keyTemplate == null || string.IsNullOrWhiteSpace(keyTemplate.Url))
            {
                continue;
            }

            serviceScheduledEndpoints.Add(new ServiceScheduledEndpoints
            {
                ServiceName = workerServiceItem.Key,
                ServiceUrl = keyTemplate.Url,
                Endpoints = null
            });
        }

        foreach (var serviceItem in serviceScheduledEndpoints)
        {
            serviceItem.Endpoints = endpoints
                .Where(x => x.ServiceName.Equals(serviceItem.ServiceName))
                .Select(s => new EndpointModel
                {
                    JobName = s.JobName,
                    Url = s.Url,
                    Method = s.Method,
                    Pattern = s.Pattern,
                    Payload = s.Payload,
                    Key = s.Key ?? defaultEndPointKey,
                    TriggerOnStartup = s.TriggerOnStartup
                })
                .ToList();
        }


        return serviceScheduledEndpoints.Where(x => x.Endpoints is { Count: > 0 }).ToList();
    }

    private static async Task SetScheduleEndpointTriggers(ISchedulerFactory schedulerFactory, List<ServiceScheduledEndpoints> checkedServiceEndpointConfigurations)
    {
        if (schedulerFactory != null && checkedServiceEndpointConfigurations is { Count: > 0 })
        {
            var scheduler = await schedulerFactory.GetScheduler();

            foreach (var serviceEndpointConfiguration in checkedServiceEndpointConfigurations)
            {
                foreach (var endpoint in serviceEndpointConfiguration.Endpoints)
                {
                    var job = JobBuilder.Create<ApiCallJob>()
                        .WithIdentity(endpoint.JobName)
                        .UsingJobData("JobName", endpoint.JobName)
                        .UsingJobData("Url", new Uri($"{serviceEndpointConfiguration.ServiceUrl}{endpoint.Url}").AbsoluteUri)
                        .UsingJobData("Method", endpoint.Method)
                        .UsingJobData("PeriodSeconds", endpoint.Payload?.PeriodSeconds ?? 0)
                        .UsingJobData("Key", endpoint.Key ?? "")
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(endpoint.JobName + "Trigger")
                        .WithCronSchedule(endpoint.Pattern) // CRON Pattern
                        .Build();

                    await scheduler.ScheduleJob(job, trigger);

                    if (endpoint.TriggerOnStartup)
                    {
                        // first run manually
                        await scheduler.TriggerJob(job.Key);
                    }
                }
            }
        }
    }
}