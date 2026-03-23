using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Responses;

public interface IApiResponseWriter
{
    Task WriteErrorAsync(HttpContext context, int statusCode, IEnumerable<string> messages, string? errorCode = null);
    Task WriteSuccessAsync<T>(HttpContext context, int statusCode, T? payload, IEnumerable<string> messages);
}