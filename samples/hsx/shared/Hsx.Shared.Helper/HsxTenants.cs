namespace Hsx.Shared.Helper;

/// <summary>
/// The tenants of the Hsx sample, which are fixed so every service knows them without asking (a real system keeps them in the identity service): the system tenant, whose users see every tenant, and the three resellers. A company and a customer are not tenants
/// (hsx/docs/TenancyModel.md).
/// </summary>
public static class HsxTenants
{
    /// <summary>The tenant of the people who run the system: they are under no isolation, see everything and write the companies, the customers and the sales.</summary>
    public static readonly Guid System = new("00000000-0000-7000-8000-00000000a001");

    /// <summary>A reseller.</summary>
    public static readonly Guid TechSummus = new("00000000-0000-7000-8000-00000000a002");

    /// <summary>A reseller.</summary>
    public static readonly Guid AudioTime = new("00000000-0000-7000-8000-00000000a003");

    /// <summary>A reseller.</summary>
    public static readonly Guid EPartner = new("00000000-0000-7000-8000-00000000a004");

    public const string SystemName = "system-tenant";

    public const string TechSummusName = "tech-summus";

    public const string AudioTimeName = "audio-time";

    public const string EPartnerName = "epartner";
}
