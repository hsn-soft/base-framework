using HsnSoft.Base.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.UI.Navigation;

public interface IMenuConfigurationContext : IServiceProviderAccessor
{
    ApplicationMenu Menu { get; }

    IAuthorizationService AuthorizationService { get; }

    IStringLocalizerFactory StringLocalizerFactory { get; }
}