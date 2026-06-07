using JetBrains.Annotations;

namespace Hhs.ContentService.EntityFrameworkCore;

public static class EfCoreDbProperties
{
    public static string DbTablePrefix { get; set; } = "";

    [CanBeNull]
    public static string DbSchema { get; set; } = null;

    public const string ConnectionStringName = "EfCore-ContentService";
}