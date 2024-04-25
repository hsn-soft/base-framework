using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.UI.Navigation;

public class MenuConfigurationContext //: IMenuConfigurationContext
{
    public MenuConfigurationContext(ApplicationMenu menu, IServiceProvider serviceProvider)
    {
        Menu = menu;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    // public IAuthorizationService AuthorizationService => _lazyServiceProvider.LazyGetRequiredService<IAuthorizationService>();

    public IStringLocalizerFactory StringLocalizerFactory => ServiceProvider.GetRequiredService<IStringLocalizerFactory>();

    public ApplicationMenu Menu { get; }

    // public Task<bool> IsGrantedAsync(string policyName)
    // {
    //     return AuthorizationService.IsGrantedAsync(policyName);
    // }

    // [CanBeNull]
    // public IStringLocalizer GetDefaultLocalizer()
    // {
    //     return StringLocalizerFactory.CreateDefaultOrNull();
    // }

    [NotNull]
    public IStringLocalizer GetLocalizer<T>()
    {
        return StringLocalizerFactory.Create<T>();
    }

    [NotNull]
    public IStringLocalizer GetLocalizer(Type resourceType)
    {
        return StringLocalizerFactory.Create(resourceType);
    }
}