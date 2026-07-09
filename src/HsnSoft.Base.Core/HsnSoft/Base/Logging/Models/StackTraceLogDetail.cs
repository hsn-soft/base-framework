using JetBrains.Annotations;

namespace HsnSoft.Base.Logging.Models;

public sealed class StackTraceLogDetail
{
    [CanBeNull]
    public string ErrorDetail { get; set; }

    [CanBeNull]
    public string StackFileName { get; set; }

    [CanBeNull]
    public string StackMethodName { get; set; }

    public int StackLineNumber { get; set; }
}