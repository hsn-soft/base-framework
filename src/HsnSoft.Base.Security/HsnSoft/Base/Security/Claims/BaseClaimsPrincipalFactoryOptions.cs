using HsnSoft.Base.Collections;

namespace HsnSoft.Base.Security.Claims;

public class BaseClaimsPrincipalFactoryOptions
{
    public BaseClaimsPrincipalFactoryOptions()
    {
        Contributors = new TypeList<IBaseClaimsPrincipalContributor>();
    }

    public ITypeList<IBaseClaimsPrincipalContributor> Contributors { get; }
}