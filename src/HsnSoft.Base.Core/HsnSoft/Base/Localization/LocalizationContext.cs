using System;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public class LocalizationContext : IServiceProviderAccessor
{
    public LocalizationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        LocalizerFactory = ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
    }

    public IStringLocalizerFactory LocalizerFactory { get; }
    public IServiceProvider ServiceProvider { get; }
}