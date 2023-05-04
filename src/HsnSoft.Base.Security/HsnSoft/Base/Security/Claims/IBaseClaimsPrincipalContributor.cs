using System.Threading.Tasks;

namespace HsnSoft.Base.Security.Claims;

public interface IBaseClaimsPrincipalContributor
{
    Task ContributeAsync(BaseClaimsPrincipalContributorContext context);
}
