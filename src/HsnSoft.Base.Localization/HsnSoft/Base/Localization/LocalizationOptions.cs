using System;
using System.Collections.Generic;
using System.Reflection;

namespace HsnSoft.Base.Localization;

/// <summary>
/// What the localization needs to know: the languages, and the layers of texts. The languages are read from the <c>Localization</c> section; the layers are added in code, <b>outermost first,
/// innermost last</b> (framework, then the shared project, then the service): a text of a later layer replaces the text of the same key of an earlier one, so the more specific layer wins.
/// </summary>
public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";

    internal static readonly string[] s_defaultSupportedLanguages = ["en", "tr", "ru"];

    private readonly List<(string Name, Func<string, string?> Read)> _layers = [];

    /// <summary>The language of a text that has no translation in the current one, and of a request that names no language. Default <c>en</c>.</summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>The languages that have texts. When it is empty: <c>en</c>, <c>tr</c>, <c>ru</c>.</summary>
    public string[] SupportedLanguages { get; set; } = [];

    internal IReadOnlyList<(string Name, Func<string, string?> Read)> Layers => _layers;

    /// <summary>
    /// Adds the texts embedded in <paramref name="assembly"/>: one file for each language, <c>Localization/{language}.json</c> (<c>&lt;EmbeddedResource Include="Localization\*.json" /&gt;</c> in the
    /// project), in the form <c>{ "culture": "tr", "texts": { "key": "text" } }</c>.
    /// </summary>
    public LocalizationOptions AddEmbedded(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        string[] resources = assembly.GetManifestResourceNames();
        return AddJson(assembly.GetName().Name ?? "assembly", language =>
        {
            string? name = Array.Find(resources, resource =>
                resource.Equals($"Localization.{language}.json", StringComparison.OrdinalIgnoreCase)
                || resource.EndsWith($".Localization.{language}.json", StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                return null;
            }

            using System.IO.Stream stream = assembly.GetManifestResourceStream(name)!;
            using System.IO.StreamReader reader = new(stream);
            return reader.ReadToEnd();
        });
    }

    /// <summary>Adds a layer of texts from any source: <paramref name="read"/> gives the JSON of a language, or <see langword="null"/> when the layer has no texts in it.</summary>
    public LocalizationOptions AddJson(string name, Func<string, string?> read)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(read);

        _layers.Add((name, read));
        return this;
    }
}
