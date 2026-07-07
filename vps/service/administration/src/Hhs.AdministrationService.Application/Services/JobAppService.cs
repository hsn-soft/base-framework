using Hhs.AdministrationService.Application.Contracts.Events;
using Hhs.AdministrationService.Application.Contracts.JobDomain;
using Hhs.AdministrationService.Application.Contracts.JobDomain.Dtos;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.AdministrationService.Application.Services;

public sealed class JobAppService : ApplicationServiceBase, IJobAppService
{
    private readonly IFrameworkLogger _logger;

    public JobAppService(IServiceProvider provider) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();
    }

    public async Task SynchAllPermissionToCacheDbTriggerAsync(SynchAllPermissionToCacheDbTriggerDto input, string correlationId = null)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: "JOB_TRIGGERED_SUCCESS",
            correlationId: correlationId,
            exception: null
        ));

        await EventBus.PublishAsync(eventMessage: new SynchAllPermissionToCacheDbEto(), correlationId: correlationId);
    }
}