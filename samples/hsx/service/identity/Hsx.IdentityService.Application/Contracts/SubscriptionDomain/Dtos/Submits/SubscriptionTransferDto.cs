using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;

namespace Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;

/// <summary>Passes a sale to another reseller: the old subscription ends and a new one starts for it.</summary>
public sealed class SubscriptionTransferDto
{
    [Required]
    [NotDefault]
    public Guid? Id { get; set; }

    /// <summary>The reseller that sells from now on.</summary>
    [Required]
    [NotDefault]
    public Guid? TenantId { get; set; }
}
