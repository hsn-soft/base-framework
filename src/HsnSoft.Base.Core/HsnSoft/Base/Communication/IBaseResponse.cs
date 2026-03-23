using System.Collections.Generic;

namespace HsnSoft.Base.Communication;

public interface IBaseResponse
{
    int StatusCode { get; }

    List<string> StatusMessages { get; }

    string StatusMessagesToSingleMessage();
}

public interface IBaseResponse<out TPayload> : IBaseResponse
{
    TPayload Payload { get; }
}