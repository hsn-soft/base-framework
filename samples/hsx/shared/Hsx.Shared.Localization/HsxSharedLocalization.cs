using HsnSoft.Base.Localization;

namespace Hsx.Shared.Localization;

/// <summary>The shared texts of the Hsx system: what more than one service says, in <c>en</c>, <c>tr</c> and <c>ru</c> (<c>Localization/{language}.json</c> of this project).</summary>
public static class HsxSharedLocalization
{
    /// <summary>
    /// Adds the shared texts as a layer after the framework's and before a service's own (the order of the calls is the order of the layers: outermost first, the innermost wins for a key that two layers have). <c>AddHsxHosting</c> does it for
    /// every Hsx host; a service only adds its own after it.
    /// </summary>
    public static LocalizationOptions AddHsxSharedTexts(this LocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.AddEmbedded(typeof(HsxSharedLocalization).Assembly);
    }
}
