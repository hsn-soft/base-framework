using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Localization;

/// <summary>
/// All the texts, merged: for each language one dictionary made from the layers (the framework, the shared project, the service...), the later layer replacing the earlier one's text of a key.
/// It is built once, when it is first asked for (the application asks at start, so a broken file stops the start), and never changes.
/// </summary>
public sealed partial class LocalizationStore
{
    private readonly Lazy<Loaded> _loaded;

    public LocalizationStore(IOptions<LocalizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        LocalizationOptions settings = options.Value;
        DefaultLanguage = settings.DefaultLanguage.Trim().ToLowerInvariant();
        SupportedLanguages = [.. (settings.SupportedLanguages.Length > 0 ? settings.SupportedLanguages : LocalizationOptions.s_defaultSupportedLanguages).Select(language => language.Trim().ToLowerInvariant()).Distinct()];
        if (!SupportedLanguages.Contains(DefaultLanguage))
        {
            throw new InvalidOperationException($"The default language '{DefaultLanguage}' is not one of the supported languages ({string.Join(", ", SupportedLanguages)}).");
        }

        _loaded = new Lazy<Loaded>(() => Load(settings), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string DefaultLanguage { get; }

    public IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>The text of <paramref name="key"/> in <paramref name="language"/>, else in the default language, else <see langword="null"/>.</summary>
    public string? Find(string language, string key)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(key);

        Loaded loaded = _loaded.Value;
        if (loaded.Merged.TryGetValue(language, out FrozenDictionary<string, string>? texts) && texts.TryGetValue(key, out string? text))
        {
            return text;
        }

        return loaded.Merged[DefaultLanguage].TryGetValue(key, out string? fallback) ? fallback : null;
    }

    /// <summary>Every text of <paramref name="language"/>, the default language's where it has none of its own.</summary>
    public IReadOnlyDictionary<string, string> All(string language)
    {
        ArgumentNullException.ThrowIfNull(language);

        Loaded loaded = _loaded.Value;
        Dictionary<string, string> all = new(loaded.Merged[DefaultLanguage]);
        if (loaded.Merged.TryGetValue(language, out FrozenDictionary<string, string>? own))
        {
            foreach (KeyValuePair<string, string> text in own)
            {
                all[text.Key] = text.Value;
            }
        }

        return all;
    }

    /// <summary>Loads the texts now (a file that cannot be read or has a key twice throws here) and tells what the translations are missing: <c>key</c> of the default language not in a language of a layer, a key that only a language has, and texts with another number of <c>{n}</c> places.</summary>
    public IReadOnlyList<string> Validate() => _loaded.Value.Problems;

    private Loaded Load(LocalizationOptions options)
    {
        List<(string Name, Func<string, string?> Read)> layers = [(typeof(LocalizationStore).Assembly.GetName().Name!, ReadOwn), .. options.Layers];
        Dictionary<string, Dictionary<string, string>> merged = SupportedLanguages.ToDictionary(language => language, _ => new Dictionary<string, string>(StringComparer.Ordinal));
        List<string> problems = [];

        foreach ((string name, Func<string, string?> read) in layers)
        {
            Dictionary<string, Dictionary<string, string>> own = [];
            foreach (string language in SupportedLanguages)
            {
                string? json = read(language);
                if (json is null)
                {
                    continue;
                }

                Dictionary<string, string> texts = Parse(name, language, json);
                own[language] = texts;
                foreach (KeyValuePair<string, string> text in texts)
                {
                    merged[language][text.Key] = text.Value;
                }
            }

            Compare(name, own, problems);
        }

        return new Loaded(merged.ToDictionary(pair => pair.Key, pair => pair.Value.ToFrozenDictionary(StringComparer.Ordinal)).ToFrozenDictionary(), problems);
    }

    private static string? ReadOwn(string language)
    {
        System.Reflection.Assembly assembly = typeof(LocalizationStore).Assembly;
        string? name = Array.Find(assembly.GetManifestResourceNames(), resource => resource.EndsWith($"Localization.{language}.json", StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            return null;
        }

        using System.IO.Stream stream = assembly.GetManifestResourceStream(name)!;
        using System.IO.StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private void Compare(string layer, Dictionary<string, Dictionary<string, string>> own, List<string> problems)
    {
        if (!own.TryGetValue(DefaultLanguage, out Dictionary<string, string>? reference))
        {
            if (own.Count > 0)
            {
                problems.Add($"{layer}: has texts in {string.Join(", ", own.Keys)} but none in the default language '{DefaultLanguage}'.");
            }

            return;
        }

        foreach ((string language, Dictionary<string, string> texts) in own.Where(pair => pair.Key != DefaultLanguage))
        {
            problems.AddRange(reference.Keys.Where(key => !texts.ContainsKey(key)).Select(key => $"{layer}: '{key}' has no text in '{language}'."));
            problems.AddRange(texts.Keys.Where(key => !reference.ContainsKey(key)).Select(key => $"{layer}: '{key}' is in '{language}' but not in '{DefaultLanguage}'."));
            problems.AddRange(texts
                .Where(pair => reference.TryGetValue(pair.Key, out string? original) && Places(original) != Places(pair.Value))
                .Select(pair => $"{layer}: '{pair.Key}' has other places ({{n}}) in '{language}' than in '{DefaultLanguage}'."));
        }

        foreach (string language in SupportedLanguages.Where(language => language != DefaultLanguage && !own.ContainsKey(language)))
        {
            problems.Add($"{layer}: has no texts in '{language}'.");
        }
    }

    private static string Places(string text) => string.Join(',', PlaceRegex().Matches(text).Select(match => match.Value).Distinct().OrderBy(place => place, StringComparer.Ordinal));

    private static Dictionary<string, string> Parse(string layer, string language, string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            string? culture = document.RootElement.TryGetProperty("culture", out JsonElement cultureElement) ? cultureElement.GetString() : null;
            if (!string.Equals(culture, language, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"its 'culture' is '{culture}', not '{language}'");
            }

            Dictionary<string, string> texts = new(StringComparer.Ordinal);
            foreach (JsonProperty text in document.RootElement.GetProperty("texts").EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(text.Name))
                {
                    throw new InvalidOperationException("a key is empty");
                }

                if (!texts.TryAdd(text.Name, text.Value.GetString() ?? string.Empty))
                {
                    throw new InvalidOperationException($"the key '{text.Name}' is there twice");
                }
            }

            return texts;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new InvalidOperationException($"The localization file of '{layer}' for '{language}' is not valid: {exception.Message}", exception);
        }
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceRegex();

    private sealed record Loaded(FrozenDictionary<string, FrozenDictionary<string, string>> Merged, List<string> Problems);
}
