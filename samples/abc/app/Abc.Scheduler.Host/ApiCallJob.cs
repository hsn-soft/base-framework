using System.Diagnostics;
using HsnSoft.Base.AspNetCore;
using Quartz;

namespace Abc.Scheduler;

/// <summary>
/// One run of a job: the call of an endpoint of a service with the key of the scheduler, the id of the run (also the correlation id, so the logs of the job and of what it caused are one chain) and the name of the job. A run that fails
/// (the service is down, it answers an error, it takes too long) is logged and the next run goes on as scheduled: the scheduler does not retry, the job is written to be called again. Two runs of a job never overlap.
/// </summary>
[DisallowConcurrentExecution]
internal sealed partial class ApiCallJob(SchedulerSettings settings, IHttpClientFactory clients, ILogger<ApiCallJob> logger) : IJob
{
    public const string ClientName = "scheduler";

    public const string JobNameKey = "job";

    public async Task Execute(IJobExecutionContext context)
    {
        ScheduledJob job = settings.Jobs.Single(candidate => string.Equals(candidate.Name, context.MergedJobDataMap.GetString(JobNameKey), StringComparison.OrdinalIgnoreCase));
        string jobId = $"job-{Guid.NewGuid():N}"[..16];
        using HttpRequestMessage request = new(new HttpMethod(job.Method.ToUpperInvariant()), new Uri(new Uri(settings.Services[job.Service]), job.Path));
        request.Headers.Add(SchedulerHeaders.ApiKey, settings.ApiKey);
        request.Headers.Add(SchedulerHeaders.JobId, jobId);
        request.Headers.Add(SchedulerHeaders.JobName, job.Name);
        request.Headers.Add("X-Correlation-Id", jobId);
        if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
        {
            request.Content = JsonContent.Create(new { jobName = job.Name, firedAt = context.FireTimeUtc, next = context.NextFireTimeUtc });
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(job.TimeoutSeconds));
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            using HttpResponseMessage response = await clients.CreateClient(ClientName).SendAsync(request, timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                LogCalled(logger, job.Name, jobId, job.Service, (int)response.StatusCode, watch.ElapsedMilliseconds);
            }
            else
            {
                LogRefused(logger, job.Name, jobId, job.Service, (int)response.StatusCode, watch.ElapsedMilliseconds);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            LogFailed(logger, exception, job.Name, jobId, job.Service, watch.ElapsedMilliseconds);
        }
    }

    [LoggerMessage(EventId = 7001, EventName = "JOB_CALLED", Level = LogLevel.Information, Message = "Job {Job} ({JobId}) called {Service}: {Status} in {Elapsed} ms")]
    private static partial void LogCalled(ILogger logger, string job, string jobId, string service, int status, long elapsed);

    [LoggerMessage(EventId = 7002, EventName = "JOB_REFUSED", Level = LogLevel.Warning, Message = "Job {Job} ({JobId}) was refused by {Service}: {Status} in {Elapsed} ms")]
    private static partial void LogRefused(ILogger logger, string job, string jobId, string service, int status, long elapsed);

    [LoggerMessage(EventId = 7003, EventName = "JOB_FAILED", Level = LogLevel.Warning, Message = "Job {Job} ({JobId}) could not call {Service} ({Elapsed} ms)")]
    private static partial void LogFailed(ILogger logger, Exception exception, string job, string jobId, string service, long elapsed);
}
