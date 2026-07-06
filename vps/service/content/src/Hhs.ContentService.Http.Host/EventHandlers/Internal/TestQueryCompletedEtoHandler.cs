using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class TestQueryCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IRequestLimitStore limitStore
) : ApplicationEventHandlerBase<TestQueryCompletedEto>(inboxStore, logger)
{
    private readonly IRequestLimitStore _limitStore = limitStore ?? throw new ArgumentNullException(nameof(limitStore));
    private const string EndpointKey = "content:jobs:test-query";

    protected override async Task ExecuteAsync(MessageEnvelope<TestQueryCompletedEto> @event, CancellationToken cancellationToken)
        => await _limitStore.DecrementAsync(EndpointKey);
}