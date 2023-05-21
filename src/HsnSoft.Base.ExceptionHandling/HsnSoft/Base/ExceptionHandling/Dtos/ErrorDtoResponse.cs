using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HsnSoft.Base.Communication;

namespace HsnSoft.Base.ExceptionHandling.Dtos;

[Serializable]
public sealed class ErrorDtoResponse : BaseResponse
{
    public override int StatusCode { get; }
    public override IEnumerable<string> StatusMessages { get; }

    private ErrorDtoResponse()
    {
    }

    public ErrorDtoResponse(int code = 0) : this()
    {
        StatusCode = code;
    }

    public ErrorDtoResponse(string message, int code = 0) : this(code)
    {
        if (!string.IsNullOrWhiteSpace(message)) StatusMessages = new[] { message };
    }

    public ErrorDtoResponse(IEnumerable<string> messages, int code = 0) : this(code)
    {
        StatusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }
}