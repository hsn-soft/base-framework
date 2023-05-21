using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Communication;

[Serializable]
public abstract class BaseResponse : IBaseResponse
{
    public abstract int StatusCode { get; }
    public abstract IEnumerable<string> StatusMessages { get; }

    public virtual string StatusMessagesToSingleMessage()
    {
        return StatusMessages.JoinAsString(", ");
    }
}

[Serializable]
public abstract class BaseResponse<TPayload> : BaseResponse, IBaseResponse<TPayload>
{
    public abstract TPayload Payload { get; }
}