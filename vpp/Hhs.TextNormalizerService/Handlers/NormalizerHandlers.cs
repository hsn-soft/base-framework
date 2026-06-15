using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Infrastructure;
using Hhs.TextNormalizerService.Services;

namespace Hhs.TextNormalizerService.Handlers;

public abstract class NormalizerEventHandlerBase<TEvent>(NormalizerInboxStore inboxStore) : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken)
    {
        if (await inboxStore.ExistsAsync(@event.EventId, cancellationToken))
            return;

        await inboxStore.SaveAsync(@event, cancellationToken);
        await ExecuteAsync(@event, cancellationToken);
    }

    protected abstract Task ExecuteAsync(TEvent @event, CancellationToken cancellationToken);
}

public sealed class CustomerNormalizeRequestCreatedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<CustomerNormalizeRequestCreatedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerNormalizeRequestCreatedEvent @event, CancellationToken cancellationToken)
        => appService.CreateCustomerNormalizeRequestAsync(@event, cancellationToken);
}

public sealed class CustomerScrapingStartedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<CustomerScrapingStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerScrapingStartedEvent @event, CancellationToken cancellationToken)
        => appService.StartCustomerScrapingAsync(@event, cancellationToken);
}

public sealed class CustomerScrapingCompletedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<CustomerScrapingCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerScrapingCompletedEvent @event, CancellationToken cancellationToken)
        => appService.CompleteCustomerScrapingAsync(@event, cancellationToken);
}

public sealed class CustomerOutlineStartedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<CustomerOutlineStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerOutlineStartedEvent @event, CancellationToken cancellationToken)
        => appService.StartCustomerOutlineAsync(@event, cancellationToken);
}

public sealed class CustomerOutlineCompletedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<CustomerOutlineCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerOutlineCompletedEvent @event, CancellationToken cancellationToken)
        => appService.CompleteCustomerOutlineAsync(@event, cancellationToken);
}

public sealed class AnalysisNormalizeRequestCreatedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<AnalysisNormalizeRequestCreatedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisNormalizeRequestCreatedEvent @event, CancellationToken cancellationToken)
        => appService.CreateAnalysisNormalizeRequestAsync(@event, cancellationToken);
}

public sealed class AnalysisItemScrapingStartedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<AnalysisItemScrapingStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemScrapingStartedEvent @event, CancellationToken cancellationToken)
        => appService.StartAnalysisItemScrapingAsync(@event, cancellationToken);
}

public sealed class AnalysisItemScrapingCompletedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<AnalysisItemScrapingCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemScrapingCompletedEvent @event, CancellationToken cancellationToken)
        => appService.CompleteAnalysisItemScrapingAsync(@event, cancellationToken);
}

public sealed class AnalysisItemOutlineStartedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<AnalysisItemOutlineStartedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemOutlineStartedEvent @event, CancellationToken cancellationToken)
        => appService.StartAnalysisItemOutlineAsync(@event, cancellationToken);
}

public sealed class AnalysisItemOutlineCompletedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<AnalysisItemOutlineCompletedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemOutlineCompletedEvent @event, CancellationToken cancellationToken)
        => appService.CompleteAnalysisItemOutlineAsync(@event, cancellationToken);
}