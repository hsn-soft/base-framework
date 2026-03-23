using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base.Communication;

[Serializable]
public class BaseResponse
{
    public int StatusCode { get; set; }

    public List<string> StatusMessages { get; set; } = [];

    [CanBeNull] public string TraceId { get; set; }

    [CanBeNull] public string ErrorCode { get; set; }

    public virtual string StatusMessagesToSingleMessage() => string.Join(", ", StatusMessages);
}

[Serializable]
public class BaseResponse<TPayload> : BaseResponse, IBaseResponse<TPayload>
{
    public TPayload Payload { get; set; }
}