using JetBrains.Annotations;

namespace Hhs.FeedRService.EntityFrameworkCore;

public static class EfCoreDbProperties
{
    public static string DbTablePrefix { get; set; } = "";

    [CanBeNull]
    public static string DbSchema { get; set; } = "public";

    public const string ConnectionStringName = "EfCore-FeedRService";
}