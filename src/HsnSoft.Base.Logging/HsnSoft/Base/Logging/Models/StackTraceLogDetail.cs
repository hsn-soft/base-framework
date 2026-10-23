namespace HsnSoft.Base.Logging.Models;

public sealed class StackTraceLogDetail
{
    public string? ErrorDetail { get; set; }

    public string? StackFileName { get; set; }

    public string? StackMethodName { get; set; }

    public int StackLineNumber { get; set; }
}
