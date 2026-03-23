using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base.Communication;

public interface IBaseResponse
{
    int StatusCode { get; }

    List<string> StatusMessages { get; }

    string StatusMessagesToSingleMessage();
}

public interface IBaseResponse<out TPayload> : IBaseResponse
{
    [CanBeNull] TPayload Payload { get; }
}