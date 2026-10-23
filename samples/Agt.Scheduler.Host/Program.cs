using Agt.Scheduler.Host;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MongoDB;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddBaseHealthChecks()
    .AddBaseMongo(builder.Configuration)   // IMongoDatabase and its readiness health check
    .Configure<SchedulerOptions>(builder.Configuration.GetSection("Scheduler"))
    .AddSingleton(TimeProvider.System)
    .AddSingleton<RecordStore>()
    .AddHostedService<RecentRecordsWorker>();

WebApplication app = builder.Build();

await app.Services.RunStartupStepAsync("record index", app.Services.GetRequiredService<RecordStore>().InitializeAsync);

// the only endpoints: what a platform probes to know the agent is alive and can reach MongoDB
app.MapBaseHealth();

await app.RunAsync();
