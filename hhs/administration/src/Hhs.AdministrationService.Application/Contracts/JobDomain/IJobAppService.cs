using Hhs.AdministrationService.Application.Contracts.JobDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task SynchAllPermissionToCacheDbTriggerAsync(SynchAllPermissionToCacheDbTriggerDto input, [CanBeNull] string correlationId = null);
}