using System.Collections.Generic;

namespace HsnSoft.Base;

/// <summary>
/// An error whose message the client may get in its own language: the hosting layer looks the key up and puts the arguments in the text. The message of the exception stays what it was written as
/// (English, what the logs say); only what is sent to the client is translated. Without a text for the key, the client gets the message.
/// </summary>
public interface ILocalizableError
{
    /// <summary>The key of the text; <see langword="null"/> when the error has none.</summary>
    string? MessageKey { get; }

    /// <summary>The values for the <c>{0}</c>, <c>{1}</c> places of the text.</summary>
    IReadOnlyList<object?> MessageArgs { get; }
}
