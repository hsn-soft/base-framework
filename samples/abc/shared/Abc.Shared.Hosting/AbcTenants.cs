namespace Abc.Shared.Hosting;

/// <summary>
/// The tenants of the Abc sample, which are fixed so every service knows them without asking: the system tenant (its users see every tenant) and one company, Acme. A real system
/// keeps its tenants in a store; the sample has two so the filters can be seen at work.
/// </summary>
public static class AbcTenants
{
    /// <summary>The tenant of the people who run the system: they see every tenant.</summary>
    public static readonly Guid System = new("00000000-0000-7000-8000-000000000001");

    /// <summary>A company that uses the system: its users see only its own data.</summary>
    public static readonly Guid Acme = new("00000000-0000-7000-8000-000000000002");
}
