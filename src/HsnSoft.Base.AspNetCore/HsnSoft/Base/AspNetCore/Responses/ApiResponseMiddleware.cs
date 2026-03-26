using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Responses;

public class ApiResponseMiddleware
{
    private readonly RequestDelegate _next;

    public ApiResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IApiResponseWriter writer, IStatusMessageProvider messageProvider)
    {
        var originalBody = context.Response.Body;

        using var memory = new MemoryStream();
        context.Response.Body = memory;

        await _next(context);

        memory.Seek(0, SeekOrigin.Begin);

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            string body = await new StreamReader(memory).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                await writer.WriteSuccessAsync<object>(context, context.Response.StatusCode, null, [messageProvider.GetMessage(context.Response.StatusCode)]);
            }
            else
            {
                object json = JsonSerializer.Deserialize<object>(body);
                await writer.WriteSuccessAsync(context, context.Response.StatusCode, json, [messageProvider.GetMessage(context.Response.StatusCode)]);
            }
        }
        else
        {
            await memory.CopyToAsync(originalBody);
        }

        context.Response.Body = originalBody;
    }
}