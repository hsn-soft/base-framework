using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Localization;

/// <summary>
/// The language the code that is running answers in. It is a short language code (<c>en</c>, <c>tr</c>), not a <c>CultureInfo</c>: the texts are chosen by the code, so it works where the
/// runtime has no culture data (a container with <c>InvariantGlobalization</c>). It follows the flow of the code (across <c>await</c>); the web host sets it for each request.
/// </summary>
public interface ILanguageAccessor
{
    /// <summary>The language of a text that has no translation in the current one (and of a request that names none).</summary>
    string DefaultLanguage { get; }

    /// <summary>The languages that have texts.</summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>The language of the flow of the code that asks: the one <see cref="Use"/> set, else <see cref="DefaultLanguage"/>.</summary>
    string Current { get; }

    /// <summary>The supported language a tag such as <c>tr-TR</c> or <c>TR</c> stands for, or <see langword="null"/> when it is not supported.</summary>
    string? Normalize(string? languageTag);

    /// <summary>Answers in <paramref name="language"/> until the returned scope is disposed (the previous language comes back).</summary>
    IDisposable Use(string language);
}
