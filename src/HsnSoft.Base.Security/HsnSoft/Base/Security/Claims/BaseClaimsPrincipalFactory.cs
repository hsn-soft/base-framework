using System.Security.Claims;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Security.Claims;

public class BaseClaimsPrincipalFactory : IBaseClaimsPrincipalFactory, ITransientDependency
{
    public BaseClaimsPrincipalFactory(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<BaseClaimsPrincipalFactoryOptions> claimOptions)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Options = claimOptions.Value;
    }

    public static string AuthenticationType => "Base.Application";

    protected IServiceScopeFactory ServiceScopeFactory { get; }
    protected BaseClaimsPrincipalFactoryOptions Options { get; }

    public virtual async Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal existsClaimsPrincipal = null)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var claimsPrincipal = existsClaimsPrincipal ?? new ClaimsPrincipal(new ClaimsIdentity(
                AuthenticationType,
                BaseClaimTypes.UserName,
                BaseClaimTypes.Role));

            var context = new BaseClaimsPrincipalContributorContext(claimsPrincipal, scope.ServiceProvider);

            foreach (var contributorType in Options.Contributors)
            {
                var contributor = (IBaseClaimsPrincipalContributor)scope.ServiceProvider.GetRequiredService(contributorType);
                await contributor.ContributeAsync(context);
            }

            return claimsPrincipal;
        }
    }
}