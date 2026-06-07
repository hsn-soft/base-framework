using Microsoft.Extensions.Hosting;

namespace Hhs.Shared.Hosting.Exceptions;

public interface IApiExceptionMapper
{
    (int StatusCode, string? ErrorCode, List<string> Messages) Map(Exception ex, IHostEnvironment env);
}