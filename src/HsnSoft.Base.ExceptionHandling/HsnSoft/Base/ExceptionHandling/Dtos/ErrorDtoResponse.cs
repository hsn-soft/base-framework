using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Communication;

namespace HsnSoft.Base.ExceptionHandling.Dtos;

[Serializable]
public sealed class ErrorDtoResponse : BaseResponse
{
    private readonly int _statusCode;
    private readonly IEnumerable<string> _statusMessages;

    public override int StatusCode => _statusCode;
    public override IEnumerable<string> StatusMessages => _statusMessages;

    private ErrorDtoResponse()
    {
    }

    public ErrorDtoResponse(int code = 0) : this()
    {
        _statusCode = code;
    }

    public ErrorDtoResponse(string message, int code = 0) : this(code)
    {
        if (!string.IsNullOrWhiteSpace(message)) _statusMessages = new[] { message };
    }

    public ErrorDtoResponse(IEnumerable<string> messages, int code = 0) : this(code)
    {
        _statusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}

[Serializable]
public sealed class ErrorDtoResponse<TPayload> : BaseResponse<TPayload>
{
    private readonly int _statusCode;
    private readonly IEnumerable<string> _statusMessages;
    private readonly TPayload _payload;

    public override int StatusCode => _statusCode;
    public override IEnumerable<string> StatusMessages => _statusMessages;
    public override TPayload Payload => _payload;

    private ErrorDtoResponse()
    {
    }

    public ErrorDtoResponse(TPayload payload, int code = 0) : this()
    {
        _payload = payload;
        _statusCode = code;
    }

    public ErrorDtoResponse(TPayload payload, string message, int code = 0) : this(payload, code)
    {
        if (!string.IsNullOrWhiteSpace(message)) _statusMessages = new[] { message };
    }
    
    public ErrorDtoResponse(TPayload payload, IEnumerable<string> messages, int code = 0) : this(payload, code)
    {
        _statusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}