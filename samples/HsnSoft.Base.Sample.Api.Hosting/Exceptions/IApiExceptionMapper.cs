using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.Sample.Api.Hosting.Exceptions;

public interface IApiExceptionMapper
{
    (int StatusCode, string ErrorCode, List<string> Messages) Map(Exception exception, IHostEnvironment environment);
}
