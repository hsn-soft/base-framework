using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Localization;

/// <summary>The default <see cref="ILanguageAccessor"/>: the language is kept per flow of the code (across <c>await</c>), the default one when nothing set it.</summary>
public sealed class LanguageAccessor(IOptions<LocalizationOptions> options) : ILanguageAccessor
{
    private readonly LocalizationOptions _options = options.Value;
    private readonly AsyncLocal<string?> _current = new();

    public string DefaultLanguage => Normalize(_options.DefaultLanguage) ?? "en";

    public IReadOnlyList<string> SupportedLanguages { get; } = [.. (options.Value.SupportedLanguages.Length > 0 ? options.Value.SupportedLanguages : LocalizationOptions.s_defaultSupportedLanguages)];

    public string Current => _current.Value ?? DefaultLanguage;

    public string? Normalize(string? languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            return null;
        }

        // "tr-TR", "tr_TR" and "TR" are all "tr": the texts are chosen by the language, not by the region
        string language = languageTag.AsSpan().Trim().ToString();
        int end = language.IndexOfAny(['-', '_']);
        language = (end > 0 ? language[..end] : language).ToLowerInvariant();
        return SupportedLanguages.Contains(language) ? language : null;
    }

    public IDisposable Use(string language)
    {
        string normalized = Normalize(language) ?? throw new ArgumentException($"'{language}' is not a supported language ({string.Join(", ", SupportedLanguages)}).", nameof(language));
        string? previous = _current.Value;
        _current.Value = normalized;
        return new Restore(() => _current.Value = previous);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
