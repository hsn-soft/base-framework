using System;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// Tells whether a failure of the database is a unique key or a unique index that a write would break, and which one, for the three providers of the framework and SQLite (the tests): PostgreSQL (state <c>23505</c>, the name of the constraint),
/// SQL Server (error 2601 or 2627, the name in the text), MySQL (error 1062, the key in the text) and SQLite (error 19, 2067 or 1555, the columns in the text). The driver of each is not referenced here: the number is read from the exception, so
/// a service that runs on one provider does not carry the others. The value that is duplicated is never read out.
/// </summary>
internal static partial class UniqueViolations
{
    public static bool TryRead(Exception? exception, out string? constraint)
    {
        constraint = null;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is not DbException database)
            {
                continue;
            }

            string text = database.Message;
            if (database.SqlState == "23505")
            {
                constraint = Property(database, "ConstraintName") ?? Match(PostgresName(), text);
                return true;
            }

            int? number = Number(database, "Number");   // SQL Server and MySQL
            if (number is 2601 or 2627)
            {
                constraint = Match(SqlServerName(), text);
                return true;
            }

            if (number == 1062)
            {
                constraint = Match(MySqlName(), text);
                return true;
            }

            if (Number(database, "SqliteExtendedErrorCode") is 2067 or 1555)
            {
                constraint = Match(SqliteName(), text);
                return true;
            }
        }

        return false;
    }

    private static string? Property(object instance, string name) => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance) as string;

    private static int? Number(object instance, string name) => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance) as int?;

    private static string? Match(Regex pattern, string text)
    {
        Match match = pattern.Match(text);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex("constraint \"([^\"]+)\"", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PostgresName();

    [GeneratedRegex("(?:constraint|index) '([^']+)'", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SqlServerName();

    [GeneratedRegex("for key '([^']+)'", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MySqlName();

    [GeneratedRegex("UNIQUE constraint failed: (.+)$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SqliteName();
}
