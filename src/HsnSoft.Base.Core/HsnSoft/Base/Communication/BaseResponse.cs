using System;
using System.Collections.Generic;

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
}

[Serializable]
public class BaseResponse<TPayload> : BaseResponse, IBaseResponse<TPayload>
{
    public TPayload Payload { get; set; }
}