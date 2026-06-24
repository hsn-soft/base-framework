using Hhs.AdministrationService.Application.Contracts.Events;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Application.Infrastructure;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.AdministrationService.EventHandlers.Internal;

public sealed class SynchAllPermissionToCacheDbEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IPermissionStoreOperationAppService permissionStoreOperationAppService
) : ApplicationEventHandlerBase<SynchAllPermissionToCacheDbEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPermissionStoreOperationAppService _permissionStoreOperationAppService = permissionStoreOperationAppService ?? throw new ArgumentNullException(nameof(permissionStoreOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<SynchAllPermissionToCacheDbEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(SynchAllPermissionToCacheDbEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _permissionStoreOperationAppService.SetParentIntegrationEvent(@event);
        await _permissionStoreOperationAppService.SynchAllPermissionToCacheDbAsync();
    }
}