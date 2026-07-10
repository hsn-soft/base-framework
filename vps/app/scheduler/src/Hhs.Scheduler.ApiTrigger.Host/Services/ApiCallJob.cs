using System.Text;
using System.Text.Json;
using Hhs.Scheduler.ApiTrigger.Host.Consts;
using Hhs.Scheduler.ApiTrigger.Host.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Quartz;

namespace Hhs.Scheduler.ApiTrigger.Host.Services;

public class ApiCallJob(IHttpClientFactory httpClientFactory, IFrameworkLogger logger) : IJob
{
    private const string SchedulerApiKeyLabel = "SchedulerApiKey";
    private const string SchedulerJobIdLabel = "SchedulerJobId";
    private const string SchedulerJobNameLabel = "SchedulerJobName";

    public async Task Execute(IJobExecutionContext context)
    {
        var jobData = context.MergedJobDataMap;
        var endpoint = new EndpointModel
        {
            JobName = jobData.GetString("JobName"),
            Url = jobData.GetString("Url"),
            Method = jobData.GetString("Method"),
            Payload = new PayloadModel { PeriodSeconds = jobData.GetInt("PeriodSeconds") },
            Key = jobData.GetString("Key")
        };

        logger.LogInformation("{WorkerName} | {JobName} | {OperationStatus}", nameof(ApiCallJob), endpoint.JobName, "START");

        var client = httpClientFactory.CreateClient();

        // if (!string.IsNullOrEmpty(bearerToken)) { client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);}
        if (!string.IsNullOrEmpty(endpoint.Key)) { client.DefaultRequestHeaders.Add(SchedulerApiKeyLabel, endpoint.Key); }

        string jobCorrelationId = $"job-{Guid.CreateVersion7():N}";
        client.DefaultRequestHeaders.Add(SchedulerJobIdLabel, jobCorrelationId);
        client.DefaultRequestHeaders.Add(SchedulerJobNameLabel, endpoint.JobName);

        string jobPeriodDesc = DescribePeriod(endpoint.Payload?.PeriodSeconds ?? 0);
        DateTime? nextTriggerTimeUtc = context.NextFireTimeUtc?.UtcDateTime;

        string payloadJson = JsonSerializer.Serialize(new
        {
            JobName = endpoint.JobName,
            JobPeriodDesc = jobPeriodDesc,
            NextTriggerTimeUtc = nextTriggerTimeUtc
        });

        try
        {
            var response = endpoint.Method?.ToUpperInvariant() switch
            {
                "POST" => await client.PostAsync(endpoint.Url, new StringContent(payloadJson, Encoding.UTF8, "application/json")),
                "GET" => await client.GetAsync(endpoint.Url),
                "PUT" => await client.PutAsync(endpoint.Url, new StringContent(payloadJson, Encoding.UTF8, "application/json")),
                "DELETE" => await client.DeleteAsync(endpoint.Url),
                _ => throw new Exception($"Method {endpoint.Method} not supported")
            };

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("{WorkerName} | {JobName} | SUCCESSFULLY TRIGGERED", nameof(ApiCallJob), endpoint.JobName);
                logger.FrameworkInfoLog(LogHelper.Generate(
                    message: $"{endpoint.JobName} successfully triggered",
                    reference: new { Type = "Job", Key = endpoint.JobName, JobPeriodDesc = jobPeriodDesc, NextTriggerTimeUtc = nextTriggerTimeUtc },
                    facility: Facilities.JobTriggeredSuccess,
                    correlationId: jobCorrelationId,
                    exception: null
                ));
            }
            else
            {
                logger.LogWarning("{WorkerName} | {JobName} | TRIGGER FAILED | {ResponseBody}", nameof(ApiCallJob), endpoint.JobName, responseBody);
                logger.FrameworkErrorLog(LogHelper.Generate(
                    message: $"{endpoint.JobName} trigger failed. Response status: {((int)response.StatusCode)} - {responseBody}",
                    reference: new { Type = "Job", Key = endpoint.JobName, JobPeriodDesc = jobPeriodDesc, NextTriggerTimeUtc = nextTriggerTimeUtc, ResponseStatusCode = (int)response.StatusCode },
                    facility: Facilities.JobTriggeredFailed,
                    correlationId: jobCorrelationId,
                    exception: new Exception(response.ReasonPhrase)
                ));
            }

            logger.LogDebug("{WorkerName} | {JobName} | TRIGGER RESULT CODE: {ResultCode}", nameof(ApiCallJob), endpoint.JobName, ((int)response.StatusCode).ToString());
            logger.LogInformation("{WorkerName} | {JobName} | {OperationStatus}", nameof(ApiCallJob), endpoint.JobName, "END");
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {JobName} | {OperationStatus} | {Error}", nameof(ApiCallJob), endpoint.JobName, "FAIL", e.Message);
            logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"{endpoint.JobName} trigger failed",
                reference: new { Type = "Job", Key = endpoint.JobName, JobPeriodDesc = jobPeriodDesc, NextTriggerTimeUtc = nextTriggerTimeUtc },
                facility: Facilities.JobTriggeredFailed,
                correlationId: jobCorrelationId,
                exception: e
            ));
        }
        finally
        {
            client.Dispose();
        }
    }

    private static string DescribePeriod(int periodSeconds)
    {
        if (periodSeconds <= 0) return "unknown";
        if (periodSeconds % 3600 == 0)
        {
            int hours = periodSeconds / 3600;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        if (periodSeconds % 60 == 0)
        {
            int minutes = periodSeconds / 60;
            return minutes == 1 ? "1 min" : $"{minutes} min";
        }

        return periodSeconds == 1 ? "1 sec" : $"{periodSeconds} sec";
    }
}