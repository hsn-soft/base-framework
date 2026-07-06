using Hhs.Shared.Contracts;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.Shared.Hosting.EventHandlers;

/// <summary>
/// Generic, framework-level handler for CachePermissionGrantsChangedEto — automatically registered
/// for every microservice via UseEventBus, replacing the identical copy previously duplicated in
/// each service's own EventHandlers folder.
/// </summary>
public sealed class CachePermissionGrantsChangedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger
) : ApplicationEventHandlerBase<CachePermissionGrantsChangedEto>(inboxStore, logger)
{
    protected override async Task ExecuteAsync(MessageEnvelope<CachePermissionGrantsChangedEto> @event, CancellationToken cancellationToken)
    {
        // Simulate a work time
        await Task.Delay(1000, cancellationToken);

        // Synch Permission Service Store background job trigger flag active
        BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore = true;
    }
}