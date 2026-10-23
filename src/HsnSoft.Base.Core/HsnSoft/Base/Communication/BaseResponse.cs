using System.Collections.Generic;

namespace HsnSoft.Base.Communication;

/// <summary>What every answer of every service is: a status, messages for people, the id of the request, and (for a failure) an error code.</summary>
public interface IBaseResponse
{
    int StatusCode { get; }

    List<string> StatusMessages { get; }

    string StatusMessagesToSingleMessage();
}

/// <summary>An answer that carries a result.</summary>
public interface IBaseResponse<out TPayload> : IBaseResponse
{
    TPayload? Payload { get; }
}

/// <summary>
/// The one type every answer derives from: a failure is a <see cref="BaseResponse"/> with an <see cref="ErrorCode"/> and no payload, a
/// success a <see cref="BaseResponse{TPayload}"/>. A caller reads <see cref="StatusCode"/> and <see cref="StatusMessages"/> the same way
/// whatever happened.
/// </summary>
public class BaseResponse : IBaseResponse
{
    public int StatusCode { get; set; }

    public List<string> StatusMessages { get; set; } = [];

    /// <summary>The identifier of the request (<c>HttpContext.TraceIdentifier</c>).</summary>
    public string? TraceId { get; set; }

    /// <summary>A machine-readable reason of a failure (<c>validation_error</c>, <c>http_404</c>, the <c>ErrorCode</c> of a business exception).</summary>
    public string? ErrorCode { get; set; }

    public virtual string StatusMessagesToSingleMessage() => string.Join(", ", StatusMessages);
}

public class BaseResponse<TPayload> : BaseResponse, IBaseResponse<TPayload>
{
    public TPayload? Payload { get; set; }
}
