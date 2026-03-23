using System;

namespace HsnSoft.Base.Serilog.Mask;

[AttributeUsage(AttributeTargets.Property)]
public class SensitiveDataAttribute : Attribute
{
    public bool PreserveLength = false;

    public int ShowFirst = 0;

    public int ShowLast = 0;

    public string? SubstituteText;

    public string Mask = "*";
}