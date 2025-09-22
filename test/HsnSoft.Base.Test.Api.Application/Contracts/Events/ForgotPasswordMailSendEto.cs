using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.Test.Api.Application.Contracts.Events;

public record ForgotPasswordMailSendEto(string UserName, string UserMailAddress, string NewPassword) : IIntegrationEventMessage
{
    public string UserName { get; } = UserName;
    public string UserMailAddress { get; } = UserMailAddress;
    public string NewPassword { get; } = NewPassword;
}