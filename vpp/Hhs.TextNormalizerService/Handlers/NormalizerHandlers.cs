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
        bool started = await inboxStore.StartAsync(@event, cancellationToken);

        if (!started)
            return;

        try
        {
            await ExecuteAsync(@event, cancellationToken);

            await inboxStore.CompleteAsync(@event.EventId, cancellationToken);
        }
        catch (Exception ex)
        {
            await inboxStore.FailAsync(@event.EventId, ex, cancellationToken);
            throw;
        }
    }

    protected abstract Task ExecuteAsync(
        TEvent @event,
        CancellationToken cancellationToken);
}

public sealed class CustomerContentCreatedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<CustomerContentCreatedEto>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerContentCreatedEto @event, CancellationToken cancellationToken)
        => appService.CreateCustomerContentNormalizeRequestAsync(@event, cancellationToken);
}

public sealed class CustomerContentNormalizeRequestCreatedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<CustomerContentNormalizeRequestCreatedEto>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken)
        => appService.StartCustomerContentNormalizeAsync(@event, cancellationToken);
}

public sealed class CustomerContentScrapingStartedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<CustomerContentScrapingStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerContentScrapingStartedEto @event, CancellationToken cancellationToken)
        => appService.StartCustomerContentScrapingAsync(@event, cancellationToken);
}

public sealed class CustomerContentScrapingCompletedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<CustomerContentScrapingCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerContentScrapingCompletedEto @event, CancellationToken cancellationToken)
        => appService.CompleteCustomerContentScrapingAsync(@event, cancellationToken);
}

public sealed class CustomerContentOutlineStartedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<CustomerContentOutlineStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerContentOutlineStartedEto @event, CancellationToken cancellationToken)
        => appService.StartCustomerContentOutlineAsync(@event, cancellationToken);
}

public sealed class OutlineProviderRequestStartedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<OutlineProviderRequestStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(OutlineProviderRequestStartedEto @event, CancellationToken cancellationToken)
        => appService.StartOutlineProviderRequestAsync(@event, cancellationToken);
}

public sealed class OutlineProviderPollingStartedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<OutlineProviderPollingStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(OutlineProviderPollingStartedEto @event, CancellationToken cancellationToken)
        => appService.ScheduleOutlineProviderPollingAsync(@event, cancellationToken);
}

public sealed class OutlineProviderCompletedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<OutlineProviderCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(OutlineProviderCompletedEto @event, CancellationToken cancellationToken)
        => appService.CompleteOutlineProviderAsync(@event, cancellationToken);
}

public sealed class CustomerContentOutlineCompletedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<CustomerContentOutlineCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(CustomerContentOutlineCompletedEto @event, CancellationToken cancellationToken)
        => appService.CompleteCustomerContentOutlineAsync(@event, cancellationToken);
}



public sealed class AnalysisNormalizeRequestCreatedEventHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService) : NormalizerEventHandlerBase<AnalysisContentCreatedEvent>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisContentCreatedEvent @event, CancellationToken cancellationToken)
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

