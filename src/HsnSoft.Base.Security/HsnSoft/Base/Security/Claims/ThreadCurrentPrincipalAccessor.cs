using System.Security.Claims;
using System.Threading;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.Security.Claims;

public class ThreadCurrentPrincipalAccessor : CurrentPrincipalAccessorBase, ISingletonDependency
{
    protected override ClaimsPrincipal GetClaimsPrincipal()
    {
        return Thread.CurrentPrincipal as ClaimsPrincipal;
    }
}