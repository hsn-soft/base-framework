using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public class LocalizableString : ILocalizableString
{
    public LocalizableString(Type resourceType, [NotNull] string name)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        ResourceType = resourceType;
    }

    [CanBeNull]
    public Type ResourceType { get; }

    [NotNull]
    public string Name { get; }

    public LocalizedString Localize(IStringLocalizerFactory stringLocalizerFactory)
    {
        return stringLocalizerFactory.Create(ResourceType)[Name];
    }

    public static LocalizableString Create<TResource>([NotNull] string name)
    {
        return new LocalizableString(typeof(TResource), name);
    }
}