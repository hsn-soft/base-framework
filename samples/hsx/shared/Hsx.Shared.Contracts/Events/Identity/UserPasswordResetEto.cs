using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Masking;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.Identity;

/// <summary>
/// An administrator asked for a new first password for a user who lost its own (or whose temporary one ran out): the system made another one-time password. The same as <see cref="UserCreatedEto"/> in what it carries and in what the notification service does with
/// it; only the words of the message differ. Published by the identity service. The old password, a temporary one too, stopped working when this was made.
/// </summary>
public sealed record UserPasswordResetEto(
    Guid UserId,
    string DisplayName,
    NotificationTypes NotificationType,
    string? Email,
    string? PhoneNumber,
    string? LanguageCode,
    [property: SensitiveData(SubstituteText = "***")] string TemporaryPassword,
    DateTimeOffset PasswordExpiresAt) : IIntegrationEventMessage;
