using System;

namespace HsnSoft.Base.Logging.Masking;

/// <summary>Marks a property whose value must not appear in logs (see <see cref="ILogMasker"/> and the logging providers' masking).</summary>
[AttributeUsage(AttributeTargets.Property)]
public class SensitiveDataAttribute : Attribute
{
    /// <summary>The mask is as long as the hidden part of the value instead of a fixed number of characters.</summary>
    public bool PreserveLength { get; set; }

    /// <summary>How many characters at the start stay visible.</summary>
    public int ShowFirst { get; set; }

    /// <summary>How many characters at the end stay visible.</summary>
    public int ShowLast { get; set; }

    /// <summary>Replaces the whole value with this text.</summary>
    public string? SubstituteText { get; set; }

    /// <summary>The character the hidden part is made of.</summary>
    public string Mask { get; set; } = MaskStrings.Default;
}
