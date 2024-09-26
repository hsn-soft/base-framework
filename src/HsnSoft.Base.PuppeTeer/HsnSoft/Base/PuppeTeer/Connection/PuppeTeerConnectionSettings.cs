using System;

namespace HsnSoft.Base.PuppeTeer.Connection;

public sealed class PuppeTeerConnectionSettings
{
    public bool Headless { get; set; } = true;
    public bool LogProcess { get; set; } = true;
    public string[] Args { get; set; } = Array.Empty<string>();
}