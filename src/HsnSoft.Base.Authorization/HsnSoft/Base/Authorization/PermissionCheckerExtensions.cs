using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Authorization;

/// <summary>The value of a constraint as a number or a flag; <see langword="null"/> when nobody gave the caller one, or it is not of that kind.</summary>
public static class PermissionCheckerExtensions
{
    public static async Task<int?> GetIntAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checker);

        return int.TryParse(await checker.GetValueAsync(permission, cancellationToken).ConfigureAwait(false), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : null;
    }

    public static async Task<long?> GetLongAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checker);

        return long.TryParse(await checker.GetValueAsync(permission, cancellationToken).ConfigureAwait(false), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : null;
    }

    public static async Task<decimal?> GetDecimalAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checker);

        return decimal.TryParse(await checker.GetValueAsync(permission, cancellationToken).ConfigureAwait(false), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : null;
    }

    public static async Task<bool?> GetBooleanAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checker);

        return bool.TryParse(await checker.GetValueAsync(permission, cancellationToken).ConfigureAwait(false), out bool value) ? value : null;
    }
}
