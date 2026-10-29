using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

/// <summary>The standard <see cref="IStringLocalizer"/> over the merged texts, in the current language. There is one set of texts, so the resource type a caller asks for does not matter.</summary>
public sealed class StoreStringLocalizer(LocalizationStore store, ILanguageAccessor language) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            string? text = store.Find(language.Current, name);
            return new LocalizedString(name, text ?? name, resourceNotFound: text is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            LocalizedString found = this[name];
            return found.ResourceNotFound ? found : new LocalizedString(name, MessageLocalizer.Format(found.Value, arguments), resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => store.All(language.Current).Select(text => new LocalizedString(text.Key, text.Value, resourceNotFound: false));
}

/// <summary><see cref="IStringLocalizer{T}"/> for code that asks for the localizer of its own class.</summary>
public sealed class StoreStringLocalizer<T>(LocalizationStore store, ILanguageAccessor language) : IStringLocalizer<T>
{
    private readonly StoreStringLocalizer _inner = new(store, language);

    public LocalizedString this[string name] => _inner[name];

    public LocalizedString this[string name, params object[] arguments] => _inner[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _inner.GetAllStrings(includeParentCultures);
}

public sealed class StoreStringLocalizerFactory(LocalizationStore store, ILanguageAccessor language) : IStringLocalizerFactory
{
    public IStringLocalizer Create(Type resourceSource) => new StoreStringLocalizer(store, language);

    public IStringLocalizer Create(string baseName, string location) => new StoreStringLocalizer(store, language);
}
