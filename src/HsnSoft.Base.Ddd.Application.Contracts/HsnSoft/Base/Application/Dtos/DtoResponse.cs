using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Communication;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public sealed class DtoResponse : BaseResponse
{
    private readonly int _statusCode;
    private readonly IEnumerable<string> _statusMessages;

    public bool Success => _statusCode is >= 200 and < 300;
    public override int StatusCode => _statusCode;
    public override IEnumerable<string> StatusMessages => _statusMessages;

    private DtoResponse()
    {
    }

    public DtoResponse(int code = 0) : this()
    {
        _statusCode = code;
    }

    public DtoResponse(string message, int code = 0) : this(code)
    {
        if (!string.IsNullOrWhiteSpace(message)) _statusMessages = new[] { message };
    }

    public DtoResponse(IEnumerable<string> messages, int code = 0) : this(code)
    {
        _statusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}

[Serializable]
public sealed class DtoResponse<TPayload> : BaseResponse<TPayload>
{
    private readonly int _statusCode;
    private readonly IEnumerable<string> _statusMessages;
    private readonly TPayload _payload;

    public bool Success => _statusCode is >= 200 and < 300;
    public override int StatusCode => _statusCode;
    public override IEnumerable<string> StatusMessages => _statusMessages;
    public override TPayload Payload => _payload;

    private DtoResponse()
    {
    }

    public DtoResponse(TPayload payload, int code = 0) : this()
    {
        _payload = payload;
        _statusCode = code;
    }

    public DtoResponse(TPayload payload, string message, int code = 0) : this(payload, code)
    {
        if (!string.IsNullOrWhiteSpace(message)) _statusMessages = new[] { message };
    }

    public DtoResponse(TPayload payload, IEnumerable<string> messages, int code = 0) : this(payload, code)
    {
        _statusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}