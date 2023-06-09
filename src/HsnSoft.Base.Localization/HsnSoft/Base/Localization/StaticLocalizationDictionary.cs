using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public class StaticLocalizationDictionary : ILocalizationDictionary
{
    public string CultureName { get; }

    protected Dictionary<string, LocalizedString> Dictionary { get; }

    public StaticLocalizationDictionary(string cultureName, Dictionary<string, LocalizedString> dictionary)
    {
        CultureName = cultureName;
        Dictionary = dictionary;
    }

    public virtual LocalizedString GetOrNull(string name)
    {
        return Dictionary.GetOrDefault(name);
    }

    public Dictionary<string, LocalizedString> GetDictionary()
    {
        return Dictionary;
    }

    public void Fill(Dictionary<string, LocalizedString> dictionary)
    {
        foreach (var item in Dictionary)
        {
            dictionary[item.Key] = item.Value;
        }
    }
}