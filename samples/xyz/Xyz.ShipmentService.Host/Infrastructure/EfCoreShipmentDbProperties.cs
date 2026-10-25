using HsnSoft.Base.EntityFrameworkCore;

namespace Xyz.ShipmentService.Infrastructure;

/// <summary>Where the shipment context (the shipments, the customers and the payments) keeps its data and on what: The table prefix and schema, the name of its connection string and the provider are in the code of the service, not in its settings.</summary>
public static class EfCoreShipmentDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;

    public static string? DbSchema { get; set; }

    /// <summary>The provider of the database (<see cref="EfCoreProviderNames"/>).</summary>
    public static string Provider { get; set; } = EfCoreProviderNames.Postgres;

    public const string ConnectionStringName = "EfCore-ShipmentService";
}
