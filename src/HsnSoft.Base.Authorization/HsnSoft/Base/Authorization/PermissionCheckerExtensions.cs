using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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

    /// <summary>
    /// The value of a constraint the caller **must** have been given: a caller that was given none gets 403 (<c>permission_constraint_not_granted</c>), before anything of the business is done, so no query is made for it. A constraint is what a role is allowed to do
    /// at most; no constraint is not "no limit" but "not allowed".
    /// </summary>
    public static async Task<string> RequireValueAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checker);

        return await checker.GetValueAsync(permission, cancellationToken).ConfigureAwait(false) ?? throw NotGranted(permission);
    }

    /// <summary>The value of a constraint as a whole number; 403 when the caller has none or it is not a number.</summary>
    public static async Task<long> RequireLongAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
        => long.TryParse(await checker.RequireValueAsync(permission, cancellationToken).ConfigureAwait(false), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : throw NotGranted(permission);

    /// <summary>The value of a constraint as a decimal; 403 when the caller has none or it is not a number.</summary>
    public static async Task<decimal> RequireDecimalAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
        => decimal.TryParse(await checker.RequireValueAsync(permission, cancellationToken).ConfigureAwait(false), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : throw NotGranted(permission);

    /// <summary>The value of a constraint as an integer; 403 when the caller has none or it is not a number.</summary>
    public static async Task<int> RequireIntAsync(this IPermissionChecker checker, string permission, CancellationToken cancellationToken = default)
        => int.TryParse(await checker.RequireValueAsync(permission, cancellationToken).ConfigureAwait(false), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : throw NotGranted(permission);

    private static BaseHttpException NotGranted(string permission)
        => new BaseHttpException(StatusCodes.Status403Forbidden, $"The constraint '{permission}' was not granted.").WithKey("permission_constraint_not_granted", permission);
}
