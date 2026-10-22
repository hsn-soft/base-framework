using System.Security.Claims;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions.ValueProviders;

public sealed class PermissionConstraintCheckContext
{
    [NotNull] public string Constraint { get; }

    [CanBeNull]  public ClaimsPrincipal Principal { get; }

    public PermissionConstraintCheckContext([NotNull]  string constraint, [CanBeNull]  ClaimsPrincipal principal)
    {
        Check.NotNull(constraint, nameof(constraint));

        Constraint = constraint;
        Principal = principal;
    }
}