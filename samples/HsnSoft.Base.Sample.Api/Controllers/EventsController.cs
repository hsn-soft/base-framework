using HsnSoft.Base.EventBus;
using HsnSoft.Base.Sample.Api.Application.Contracts.Events;
using Microsoft.AspNetCore.Mvc;

namespace HsnSoft.Base.Sample.Api.Controllers;

public sealed record PublishForgotPasswordDto(string UserName, string UserMailAddress);

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventBus eventBus) : ControllerBase
{
    /// <summary>Publishes a <see cref="ForgotPasswordMailSendEto"/> and returns the correlation id the event was published with.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<string> PublishForgotPasswordAsync([FromBody] PublishForgotPasswordDto input)
    {
        string correlationId = Guid.CreateVersion7().ToString("N");
        await eventBus.PublishAsync(new ForgotPasswordMailSendEto(input.UserName, input.UserMailAddress, "TempPassword1!"), correlationId: correlationId);
        return correlationId;
    }
}
