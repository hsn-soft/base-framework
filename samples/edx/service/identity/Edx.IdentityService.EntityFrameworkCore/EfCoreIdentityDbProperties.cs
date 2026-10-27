using HsnSoft.Base.EntityFrameworkCore;

namespace Edx.IdentityService.EntityFrameworkCore;

/// <summary>Where the identity context keeps its data and on what: the table prefix and schema, the name of its connection string, and the provider. They are in the code of the service, not in its settings.</summary>
public static class EfCoreIdentityDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;

    public static string? DbSchema { get; set; }

    /// <summary>The provider of the identity database (<see cref="EfCoreProviderNames"/>).</summary>
    public static string Provider { get; set; } = EfCoreProviderNames.SqlServer;

    public const string ConnectionStringName = "EfCore-IdentityService";
}
