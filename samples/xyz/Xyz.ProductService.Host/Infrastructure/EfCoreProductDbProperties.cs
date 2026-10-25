using HsnSoft.Base.EntityFrameworkCore;

namespace Xyz.ProductService.Infrastructure;

/// <summary>Where the product context keeps its data and on what: The table prefix and schema, the name of its connection string and the provider are in the code of the service, not in its settings.</summary>
public static class EfCoreProductDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;

    public static string? DbSchema { get; set; }

    /// <summary>The provider of the database (<see cref="EfCoreProviderNames"/>).</summary>
    public static string Provider { get; set; } = EfCoreProviderNames.Postgres;

    public const string ConnectionStringName = "EfCore-ProductService";
}
