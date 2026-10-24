using System;

namespace HsnSoft.Base.Logging.Masking;

/// <summary>Hides sensitive values (passwords, tokens, properties marked with <see cref="SensitiveDataAttribute"/>) before something is logged.</summary>
public interface ILogMasker
{
    /// <summary>Masks the values of sensitive keys in a JSON text; text that is not JSON is returned as it is.</summary>
    string MaskText(string? text);

    /// <summary>
    /// Masks a JSON text that is the serialization of <paramref name="shape"/>: besides the usual keys, the values of the properties of
    /// that class (and of the classes inside it) that carry <see cref="SensitiveDataAttribute"/>, as the attribute says.
    /// </summary>
    string MaskText(string? text, Type? shape) => MaskText(text);

    /// <summary>A copy of the object with its sensitive properties masked.</summary>
    object MaskObject<T>(T value);
}
