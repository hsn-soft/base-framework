namespace HsnSoft.Base.Localization;

public static class MessageLocalizerExtensions
{
    /// <summary>
    /// The text of <paramref name="key"/> in the current language with the arguments put in, and the key itself when no language has it (so a label in a response body is never empty). For the
    /// code that puts a translated text in a response (an application service, a controller): <c>localizer.Get("OrderStatus:Pending")</c>.
    /// </summary>
    public static string Get(this IMessageLocalizer localizer, string key, params object?[] args)
    {
        System.ArgumentNullException.ThrowIfNull(localizer);

        return localizer.Find(key, args) ?? key;
    }
}
