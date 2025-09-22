using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Test.Api.Application.Contracts.Events;

namespace HsnSoft.Base.Test.Api.EventHandlers;

public sealed class ForgotPasswordMailSendEtoHandler : IIntegrationEventHandler<ForgotPasswordMailSendEto>
{
    // private readonly IBaseLogger _logger;
    // private readonly IAuthAppService _authAppService;
    //
    // public ForgotPasswordMailSendEtoHandler(IBaseLogger logger, IAuthAppService authAppService)
    // {
    //     _logger = logger;
    //     _authAppService = authAppService;
    // }

    public async Task HandleAsync(MessageEnvelope<ForgotPasswordMailSendEto> @event)
    {
        // _logger.LogDebug("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
        //     @event.Producer,
        //     nameof(ForgotPasswordMailSendEto)[..^"Eto".Length],
        //     @event.CorrelationId ?? string.Empty,
        //     @event.MessageId.ToString(),
        //     @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);
        //
        // _authAppService.SetParentIntegrationEvent(@event);
        // await _authAppService.SendForgotPasswordMailAsync(@event.Message);
    }
}