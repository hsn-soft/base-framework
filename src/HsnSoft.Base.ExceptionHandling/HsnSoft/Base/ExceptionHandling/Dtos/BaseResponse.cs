using System;
using System.Collections.Generic;
using System.Linq;

namespace HsnSoft.Base.ExceptionHandling.Dtos;

public abstract class BaseResponse
{
    public bool Success { get; }
    public string[] Messages { get; }
    public int Code { get; }

    private BaseResponse(bool success, int code)
    {
        Success = success;
        Messages = Array.Empty<string>();
        Code = code;
    }

    protected BaseResponse(bool success, string message, int code = 0) : this(success, code)
    {
        if (!string.IsNullOrWhiteSpace(message)) Messages = new[] { message };
    }

    protected BaseResponse(bool success, IEnumerable<string> messages, int code = 0) : this(success, code)
    {
        Messages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}