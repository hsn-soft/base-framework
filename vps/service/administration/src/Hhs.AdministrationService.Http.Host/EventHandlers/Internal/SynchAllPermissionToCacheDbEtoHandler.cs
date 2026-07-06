using Hhs.AdministrationService.Application.Contracts.Events;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.AdministrationService.EventHandlers.Internal;

public sealed class SynchAllPermissionToCacheDbEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IPermissionStoreOperationAppService permissionStoreOperationAppService
) : ApplicationEventHandlerBase<SynchAllPermissionToCacheDbEto>(inboxStore, logger, permissionStoreOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<SynchAllPermissionToCacheDbEto> @event, CancellationToken cancellationToken)
        => await permissionStoreOperationAppService.SynchAllPermissionToCacheDbAsync();
}