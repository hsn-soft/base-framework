using Hhs.AdministrationService.Application.Contracts.Events;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.AdministrationService.EventHandlers.Internal;

public sealed class SynchAllPermissionToCacheDbEtoHandler(
    IAppConsoleLogger logger,
    IPermissionStoreOperationAppService permissionStoreOperationAppService
) : IIntegrationEventHandler<SynchAllPermissionToCacheDbEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPermissionStoreOperationAppService _permissionStoreOperationAppService = permissionStoreOperationAppService ?? throw new ArgumentNullException(nameof(permissionStoreOperationAppService));

    public async Task HandleAsync(MessageEnvelope<SynchAllPermissionToCacheDbEto> @event)
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