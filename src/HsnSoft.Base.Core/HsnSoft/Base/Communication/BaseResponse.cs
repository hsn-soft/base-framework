using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HsnSoft.Base.Communication;

[Serializable]
public class BaseResponse : IBaseResponse
{
    public int StatusCode { get; set; }
    public List<string> StatusMessages { get; set; }

    public virtual string StatusMessagesToSingleMessage()
    {
        return StatusMessages.JoinAsString(", ");
    }

    public virtual string ToJsonString()
    {
        return JsonSerializer.Serialize(this);
    }
}

[Serializable]
public class BaseResponse<TPayload> : BaseResponse, IBaseResponse<TPayload>
{
    public TPayload Payload { get; set; }
}