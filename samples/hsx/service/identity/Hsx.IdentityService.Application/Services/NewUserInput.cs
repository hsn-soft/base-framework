using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// What is known of a user to be made: no password (the system makes it, <see cref="Security.TemporaryPasswordGenerator"/>). The email is the sign-in, so it is always given; <paramref name="NotificationType"/> says how the user is told its first
/// password: by an email, or by a text message to <paramref name="PhoneNumber"/> (then it is given).
/// </summary>
public sealed record NewUserInput(string UserName, string DisplayName, string Email, NotificationTypes NotificationType, string? PhoneNumber = null, string? LanguageCode = null);
