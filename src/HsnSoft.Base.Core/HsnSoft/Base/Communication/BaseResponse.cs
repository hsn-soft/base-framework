using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HsnSoft.Base.Communication;

[Serializable]
public abstract class BaseResponse : IBaseResponse
{
    public abstract int StatusCode { get; }
    public abstract List<string> StatusMessages { get; }

    public virtual string StatusMessagesToSingleMessage()
    {
        return StatusMessages.JoinAsString(", ");
    }
    
    public virtual  string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }
}

[Serializable]
public abstract class BaseResponse<TPayload> : BaseResponse, IBaseResponse<TPayload>
{
    public abstract TPayload Payload { get; }
}