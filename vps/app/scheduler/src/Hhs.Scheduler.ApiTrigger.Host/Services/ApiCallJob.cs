using System.Text;
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
            Body = jobData.GetString("Body"),
            Key = jobData.GetString("Key")
        };

        logger.LogInformation("{WorkerName} | {JobName} | {OperationStatus}", nameof(ApiCallJob), endpoint.JobName, "START");

        var client = httpClientFactory.CreateClient();

        // if (!string.IsNullOrEmpty(bearerToken)) { client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);}
        if (!string.IsNullOrEmpty(endpoint.Key)) { client.DefaultRequestHeaders.Add(SchedulerApiKeyLabel, endpoint.Key); }

        string jobCorrelationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add(SchedulerJobIdLabel, jobCorrelationId);
        client.DefaultRequestHeaders.Add(SchedulerJobNameLabel, endpoint.JobName);

        try
        {
            var response = endpoint.Method?.ToUpperInvariant() switch
            {
                "POST" => await client.PostAsync(endpoint.Url, new StringContent(endpoint.Body ?? "", Encoding.UTF8, "application/json")),
                "GET" => await client.GetAsync(endpoint.Url),
                "PUT" => await client.PutAsync(endpoint.Url, new StringContent(endpoint.Body ?? "", Encoding.UTF8, "application/json")),
                "DELETE" => await client.DeleteAsync(endpoint.Url),
                _ => throw new Exception($"Method {endpoint.Method} not supported")
            };

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("{WorkerName} | {JobName} | SUCCESSFULLY TRIGGERED", nameof(ApiCallJob), endpoint.JobName);
                logger.FrameworkInfoLog(LogHelper.Generate(
                    message: $"{endpoint.JobName} successfully triggered",
                    reference: new { RefContentId = endpoint.JobName },
                    facility: "JOB_TRIGGERED_SUCCESS",
                    correlationId: jobCorrelationId,
                    exception: null
                ));
            }
            else
            {
                logger.LogWarning("{WorkerName} | {JobName} | TRIGGER FAILED | {ResponseBody}", nameof(ApiCallJob), endpoint.JobName, responseBody);
                logger.FrameworkErrorLog(LogHelper.Generate(
                    message: $"{endpoint.JobName} trigger failed. Response status: {((int)response.StatusCode)} - {responseBody}",
                    reference: new { RefContentId = endpoint.JobName },
                    facility: "JOB_TRIGGERED_FAILED",
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
                reference: new { RefContentId = endpoint.JobName },
                facility: "JOB_TRIGGERED_FAILED",
                correlationId: jobCorrelationId,
                exception: e
            ));
        }
        finally
        {
            client.Dispose();
        }
    }
}