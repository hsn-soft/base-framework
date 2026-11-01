namespace Hsx.Shared.Helper;

/// <summary>
/// The tenants of the Hsx sample, which are fixed so every service knows them without asking (a real system keeps them in the identity service): the system tenant, whose users see every tenant; two resellers, Techsummus and Pixel, that sell products;
/// and Acme, an account of Techsummus (a tenant under a tenant, so a user of Techsummus sees it).
/// </summary>
public static class HsxTenants
{
    /// <summary>The tenant of the people who run the system: they are under no isolation and see everything.</summary>
    public static readonly Guid System = new("00000000-0000-7000-8000-00000000a001");

    /// <summary>A reseller: its users see its own data and the data of the tenants under it.</summary>
    public static readonly Guid Techsummus = new("00000000-0000-7000-8000-00000000a002");

    /// <summary>Another reseller: it sold the same customer another product.</summary>
    public static readonly Guid Pixel = new("00000000-0000-7000-8000-00000000a003");

    /// <summary>An account of Techsummus.</summary>
    public static readonly Guid Acme = new("00000000-0000-7000-8000-00000000a004");

    public const string SystemName = "system";

    public const string TechsummusName = "techsummus";

    public const string PixelName = "pixel";

    public const string AcmeName = "acme";
}
