namespace HsnSoft.Base.Localization;

/// <summary>Finds the text of a key in the current language (<see cref="ILanguageAccessor.Current"/>).</summary>
public interface IMessageLocalizer
{
    /// <summary>
    /// The text of <paramref name="key"/> with <paramref name="args"/> put in its <c>{0}</c>, <c>{1}</c> places: in the current language, else in the default language;
    /// <see langword="null"/> when no language has it, so the caller says what it has (the exception's own message).
    /// </summary>
    string? Find(string key, params object?[] args);
}
