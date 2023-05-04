using System.Security.Claims;
using System.Threading.Tasks;

namespace HsnSoft.Base.Security.Claims;

public interface IBaseClaimsPrincipalFactory
{
    Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal existsClaimsPrincipal = null);
}
