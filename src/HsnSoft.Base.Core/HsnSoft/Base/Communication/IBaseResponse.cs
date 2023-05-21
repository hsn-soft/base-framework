using System.Collections.Generic;

namespace HsnSoft.Base.Communication;

public interface IBaseResponse
{
    public int StatusCode { get; }

    public IEnumerable<string> StatusMessages { get; }

    string StatusMessagesToSingleMessage();
}

public interface IBaseResponse<out TPayload> : IBaseResponse
{
    public TPayload Payload { get; }
}