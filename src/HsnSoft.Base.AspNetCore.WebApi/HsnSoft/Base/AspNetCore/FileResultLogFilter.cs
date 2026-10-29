using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HsnSoft.Base.AspNetCore;

/// <summary>
/// Tells the request logging that the action answered with a file (<c>File(...)</c>, <c>PhysicalFile(...)</c>), whatever its content type:
/// a JSON document served as a file is a file, not a JSON answer. Without this the middleware can only tell a file by its
/// <c>Content-Disposition</c> or a binary content type.
/// </summary>
public sealed class FileResultLogFilter : IResultFilter
{
    internal const string ItemKey = "HsnSoft.Base.AspNetCore.DownloadedFile";

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is FileResult file)
        {
            context.HttpContext.Items[ItemKey] = new DownloadedFile(string.IsNullOrWhiteSpace(file.FileDownloadName) ? null : file.FileDownloadName);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    internal sealed record DownloadedFile(string? FileName);
}
