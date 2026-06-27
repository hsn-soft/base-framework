using AutoMapper;
using Hhs.ContentService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Hhs.ContentService.Application;

public abstract class ApplicationServiceBase : ApplicationService, IEventApplicationService
{
    [NotNull]
    protected IStringLocalizer L { get; }

    [NotNull]
    protected IMapper Mapper { get; }

    // KNOWN GAP: EventBus.PublishAsync is called after DB writes with no transactional guarantee.
    // If a publish fails after a successful DB commit the event is permanently lost.
    // Mitigation: implement an Outbox table + relay worker in each service.
    [NotNull]
    protected IEventBus EventBus { get; }

    [CanBeNull]
    protected ParentMessageEnvelope ParentIntegrationEvent { get; private set; }

    protected ApplicationServiceBase(IServiceProvider provider) : base(provider)
    {
        var serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider), "ApplicationServiceBase IServiceProvider is null");
        Mapper = serviceProvider.GetRequiredService<IMapper>();
        EventBus = serviceProvider.GetRequiredService<IEventBus>();

        L = StringLocalizerFactory.CreateMultiple([
            typeof(ContentServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]);
    }

    public void SetParentIntegrationEvent<T>(MessageEnvelope<T> @event) where T : IIntegrationEventMessage
    {
        ParentIntegrationEvent = JsonConvert.DeserializeObject<ParentMessageEnvelope>(JsonConvert.SerializeObject(@event));
    }
}