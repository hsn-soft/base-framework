using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public sealed class DefaultStringLocalizer : IStringLocalizer
{
    private readonly Type _resourceType;

    public DefaultStringLocalizer(Type resourceType)
    {
        _resourceType = resourceType ?? typeof(DefaultResource);
    }

    public LocalizedString this[string name]
    {
        get
        {
            string value = GetLocalizationDictionary().GetOrNull(name);
            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var actualValue = this[name];
            return !actualValue.ResourceNotFound
                ? new LocalizedString(name, string.Format(actualValue.Value, arguments), false)
                : actualValue;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return GetLocalizationDictionary().GetDictionary().Select(x => x.Value).ToList();
    }

    private ILocalizationDictionary GetLocalizationDictionary()
    {
        // string test = CultureInfo.CurrentUICulture.Name;
        var cultureName = Thread.CurrentThread.CurrentCulture.Name;

        //Try to get from same language dictionary (without country code)
        if (cultureName.Contains('-')) //Example: "tr-TR" (length=5)
        {
            cultureName = CultureHelper.GetBaseCultureName(cultureName);
        }

        var localizationDictionary = new ServiceResourceDictionary(cultureName);
        localizationDictionary.ImportJsonResourceToDictionary(typeof(DefaultResource));
        localizationDictionary.ImportJsonResourceToDictionary(_resourceType);

        return localizationDictionary;
    }
}