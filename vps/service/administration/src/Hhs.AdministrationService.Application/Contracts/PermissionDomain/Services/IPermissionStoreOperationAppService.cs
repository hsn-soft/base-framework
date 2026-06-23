using HsnSoft.Base.EventBus;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;

public interface IPermissionStoreOperationAppService : IEventApplicationService
{
    Task SynchAllPermissionToCacheDbAsync();
}