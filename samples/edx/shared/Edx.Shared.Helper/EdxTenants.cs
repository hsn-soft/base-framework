namespace Edx.Shared.Helper;

/// <summary>
/// The tenants of the Edx sample, which are fixed so every service knows them without asking: the system tenant (its users see every tenant) and two customers who send their files, Veriban and Bilgera. A real system
/// keeps its tenants in a store (here, the identity service does, and seeds these three); the ids are constants so the code that has to name one does not have to look it up.
/// </summary>
public static class EdxTenants
{
    /// <summary>The tenant of the people who run the system: its users are not held back by the tenant filters and see every tenant.</summary>
    public static readonly Guid System = new("00000000-0000-7000-8000-00000000e001");

    /// <summary>A customer: its users see only its own data.</summary>
    public static readonly Guid Veriban = new("00000000-0000-7000-8000-00000000e002");

    /// <summary>Another customer.</summary>
    public static readonly Guid Bilgera = new("00000000-0000-7000-8000-00000000e003");

    /// <summary>The codes a user names its tenant by when it signs in.</summary>
    public const string SystemCode = "system-tenant";

    public const string VeribanCode = "veriban";

    public const string BilgeraCode = "bilgera";
}
