using System;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public sealed class DefaultStringLocalizerFactory : IStringLocalizerFactory, IStringLocalizerFactoryWithDefaultResourceSupport
{
    public IStringLocalizer Create(Type resourceType) => new DefaultStringLocalizer(resourceType);

    public IStringLocalizer Create(string baseName, string location) => new DefaultStringLocalizer(typeof(DefaultResource));

    public IStringLocalizer CreateDefaultOrNull()
    {
        return Create(typeof(DefaultResource));
    }
}