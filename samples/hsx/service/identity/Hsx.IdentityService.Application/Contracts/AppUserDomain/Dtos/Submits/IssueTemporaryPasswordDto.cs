using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;

/// <summary>Asks the system to make a new temporary password for a user who lost its own, and to tell the user. No person sees or chooses it.</summary>
public sealed class IssueTemporaryPasswordDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    /// <summary>How the user is told: an email, or a text message (the user then has a phone number).</summary>
    public NotificationTypes NotificationType { get; set; } = NotificationTypes.Email;
}
