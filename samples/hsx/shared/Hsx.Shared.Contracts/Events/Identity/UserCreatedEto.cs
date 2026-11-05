using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Masking;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.Identity;

/// <summary>
/// A user was made, and the system made a one-time password for it that no person knows. Published by the identity service; the notification service sends it in the message of <paramref name="NotificationType"/> (an email to <paramref name="Email"/>,
/// or a text message to <paramref name="PhoneNumber"/>) in the language of the user, with the warnings that it is valid until <paramref name="PasswordExpiresAt"/> and must be changed at the first sign-in. The password is in the clear on purpose (the message
/// is how the person gets it): it is never logged (<see cref="SensitiveDataAttribute"/>), and no consumer keeps it. The tenant travels in the envelope.
/// </summary>
public sealed record UserCreatedEto(
    Guid UserId,
    string DisplayName,
    NotificationTypes NotificationType,
    string? Email,
    string? PhoneNumber,
    string? LanguageCode,
    [property: SensitiveData(SubstituteText = "***")] string TemporaryPassword,
    DateTimeOffset PasswordExpiresAt) : IIntegrationEventMessage;
