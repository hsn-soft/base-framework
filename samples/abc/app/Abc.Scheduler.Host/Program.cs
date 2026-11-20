using Abc.Scheduler;
using HsnSoft.Base.AspNetCore;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddBaseHosting();   // the .env file, the port, the limits of Kestrel and the time to stop: the section Hosting
builder.Logging.AddBaseSerilog(builder.Configuration, "Abc.Scheduler");

builder.Services
    .AddBaseLogging()
    .AddBaseHealthChecks(builder.Configuration)   // /health/live, /startup and /ready
                                                  // the key the scheduler calls with: Scheduler:ApiKey, from the environment, the .env file or the secret of the platform; the host does not start without it
    .RequireBaseSettings("Scheduler:ApiKey")
    // the jobs of Scheduler:Jobs, one cron trigger each (a section that cannot work stops the host here)
    .AddSchedulerJobs(builder.Configuration);

// a failure to start (a key that is missing, a job for a service that is not listed) is logged as critical and ends the process with the exit code 1
return await builder.RunBaseAsync(app =>
{
    app.MapBaseHealth();
    app.MapBaseInfo();
});
