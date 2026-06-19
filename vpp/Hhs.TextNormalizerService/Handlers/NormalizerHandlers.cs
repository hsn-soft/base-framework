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

public sealed class AnalysisContentCreatedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<AnalysisContentCreatedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisContentCreatedEto @event, CancellationToken cancellationToken)
        => appService.CreateAnalysisContentNormalizeRequestAsync(@event, cancellationToken);
}

public sealed class AnalysisContentNormalizeRequestCreatedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<AnalysisContentNormalizeRequestCreatedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisContentNormalizeRequestCreatedEto @event, CancellationToken cancellationToken)
        => appService.StartAnalysisContentNormalizeAsync(@event, cancellationToken);
}

public sealed class AnalysisItemScrapingStartedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<AnalysisItemScrapingStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemScrapingStartedEto @event, CancellationToken cancellationToken)
        => appService.StartAnalysisItemScrapingAsync(@event, cancellationToken);
}

public sealed class AnalysisItemScrapingCompletedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<AnalysisItemScrapingCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemScrapingCompletedEto @event, CancellationToken cancellationToken)
        => appService.CompleteAnalysisItemScrapingAsync(@event, cancellationToken);
}

public sealed class AnalysisItemOutlineStartedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<AnalysisItemOutlineStartedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemOutlineStartedEto @event, CancellationToken cancellationToken)
        => appService.StartAnalysisItemOutlineAsync(@event, cancellationToken);
}

public sealed class AnalysisItemOutlineCompletedEtoHandler(NormalizerInboxStore inboxStore, NormalizerOperationAppService appService)
    : NormalizerEventHandlerBase<AnalysisItemOutlineCompletedEto>(inboxStore)
{
    protected override Task ExecuteAsync(AnalysisItemOutlineCompletedEto @event, CancellationToken cancellationToken)
        => appService.CompleteAnalysisItemOutlineAsync(@event, cancellationToken);
}