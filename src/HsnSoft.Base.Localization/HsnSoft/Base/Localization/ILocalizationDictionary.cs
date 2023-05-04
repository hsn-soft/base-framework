using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

/// <summary>
/// Represents a dictionary that is used to find a localized string.
/// </summary>
public interface ILocalizationDictionary
{
    string CultureName { get; }

    LocalizedString GetOrNull(string name);

    Dictionary<string, LocalizedString> GetDictionary();

    void Fill(Dictionary<string, LocalizedString> dictionary);
}