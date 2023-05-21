using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Communication;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public sealed class DtoResponse : BaseResponse
{
    public override int StatusCode { get; }
    public override IEnumerable<string> StatusMessages { get; }

    private DtoResponse()
    {
    }

    public DtoResponse(int code = 0) : this()
    {
        StatusCode = code;
    }

    public DtoResponse(string message, int code = 0) : this(code)
    {
        if (!string.IsNullOrWhiteSpace(message)) StatusMessages = new[] { message };
    }

    public DtoResponse(IEnumerable<string> messages, int code = 0) : this(code)
    {
        StatusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}

[Serializable]
public sealed class DtoResponse<TPayload> : BaseResponse<TPayload>
{
    public override int StatusCode { get; }
    public override IEnumerable<string> StatusMessages { get; }
    public override TPayload Payload { get; }

    private DtoResponse()
    {
    }

    public DtoResponse(TPayload payload, int code = 0) : this()
    {
        Payload = payload;
        StatusCode = code;
    }

    public DtoResponse(TPayload payload, string message, int code = 0) : this(payload, code)
    {
        if (!string.IsNullOrWhiteSpace(message)) StatusMessages = new[] { message };
    }

    public DtoResponse(TPayload payload, IEnumerable<string> messages, int code = 0) : this(payload, code)
    {
        StatusMessages = messages.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
}