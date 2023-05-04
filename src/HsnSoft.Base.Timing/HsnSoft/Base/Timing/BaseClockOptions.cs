using System;

namespace HsnSoft.Base.Timing;

public class BaseClockOptions
{
    /// <summary>
    /// Default: <see cref="DateTimeKind.Unspecified"/>
    /// </summary>
    public DateTimeKind Kind { get; set; }

    public BaseClockOptions()
    {
        Kind = DateTimeKind.Unspecified;
    }
}