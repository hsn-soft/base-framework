using Quartz;

namespace Abc.Scheduler;

internal static class SchedulerRegistration
{
    /// <summary>
    /// Registers the jobs of <c>Scheduler:Jobs</c> with Quartz: one cron trigger for each, and one that fires at the start for a job that asks for it. A section that cannot work (a job for a service that is not listed, a wrong cron)
    /// stops the host here, with every reason, not at the time the job should have run.
    /// </summary>
    public static IServiceCollection AddSchedulerJobs(this IServiceCollection services, IConfiguration configuration)
    {
        SchedulerSettings settings = configuration.GetSection(SchedulerSettings.SectionName).Get<SchedulerSettings>() ?? new SchedulerSettings();
        settings.Validate();

        services.AddSingleton(settings);
        services.AddHttpClient(ApiCallJob.ClientName);
        services.AddQuartz(quartz =>
        {
            foreach (ScheduledJob job in settings.Jobs)
            {
                JobKey key = new(job.Name);
                quartz.AddJob<ApiCallJob>(options => options.WithIdentity(key).UsingJobData(ApiCallJob.JobNameKey, job.Name).StoreDurably());
                quartz.AddTrigger(trigger => trigger.ForJob(key).WithIdentity($"{job.Name}-cron").WithCronSchedule(job.Cron));
                if (job.TriggerOnStartup)
                {
                    quartz.AddTrigger(trigger => trigger.ForJob(key).WithIdentity($"{job.Name}-startup").StartNow());
                }
            }
        });

        // a stop waits for the run that is going on
        return services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }
}
