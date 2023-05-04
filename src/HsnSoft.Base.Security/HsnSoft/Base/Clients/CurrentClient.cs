using System.Security.Principal;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Clients;

public class CurrentClient : ICurrentClient, ITransientDependency
{
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public CurrentClient(ICurrentPrincipalAccessor principalAccessor)
    {
        _principalAccessor = principalAccessor;
    }

    public virtual string Id => _principalAccessor.Principal?.FindClientId();

    public virtual bool IsAuthenticated => Id != null;
}