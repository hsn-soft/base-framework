using System.ComponentModel.DataAnnotations;

namespace Hsx.Shared.Helper;

/// <summary>How a person is told something: the notification service sends a message of this kind to the address in the event.</summary>
public enum NotificationTypes
{
    /// <summary>An email, to the email address of the person.</summary>
    [Display(Name = "Email", Prompt = "email")]
    Email = 1,

    /// <summary>A text message, to the phone number of the person.</summary>
    [Display(Name = "Sms", Prompt = "sms")]
    Sms = 2,
}
