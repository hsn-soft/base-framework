using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Domain.Enums;

/// <summary>What a tenant is. A company and a customer are not tenants (the old Account tenant is gone: a company and a customer are data of a reseller).</summary>
public enum TenantTypes
{
    [Display(Name = "Unknown", Prompt = "unknown")]
    Unknown = 0,

    /// <summary>The tenant of the people who run the system.</summary>
    [Display(Name = "System", Prompt = "system")]
    System = 1,

    /// <summary>A company that sells the products to customers.</summary>
    [Display(Name = "Reseller", Prompt = "reseller")]
    Reseller = 2,
}
