using System;
using System.Security.Claims;
using System.Threading;

namespace HsnSoft.Base.Security.Claims;

public abstract class CurrentPrincipalAccessorBase : ICurrentPrincipalAccessor
{
    private readonly AsyncLocal<ClaimsPrincipal> _currentPrincipal = new AsyncLocal<ClaimsPrincipal>();
    public ClaimsPrincipal Principal => _currentPrincipal.Value ?? GetClaimsPrincipal();

    public virtual IDisposable Change(ClaimsPrincipal principal)
    {
        return SetCurrent(principal);
    }

    protected abstract ClaimsPrincipal GetClaimsPrincipal();

    private IDisposable SetCurrent(ClaimsPrincipal principal)
    {
        var parent = Principal;
        _currentPrincipal.Value = principal;
        return new DisposeAction(() =>
        {
            _currentPrincipal.Value = parent;
        });
    }
}