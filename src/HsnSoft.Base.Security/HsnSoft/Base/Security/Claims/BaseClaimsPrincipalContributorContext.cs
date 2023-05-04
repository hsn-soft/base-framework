using System;
using System.Security.Claims;
using JetBrains.Annotations;

namespace HsnSoft.Base.Security.Claims;

public class BaseClaimsPrincipalContributorContext
{
    [NotNull]
    public ClaimsPrincipal ClaimsPrincipal { get; }

    [NotNull]
    public IServiceProvider ServiceProvider { get; }

    public BaseClaimsPrincipalContributorContext(
        [NotNull] ClaimsPrincipal claimsIdentity,
        [NotNull] IServiceProvider serviceProvider)
    {
        ClaimsPrincipal = claimsIdentity;
        ServiceProvider = serviceProvider;
    }
}
