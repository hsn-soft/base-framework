using CoreWCF;
using Edx.Shared.Efatura.Wire;

namespace Edx.Shared.Efatura.Server;

/// <summary>A service of the exchange (the center, a post box) refuses a call before it queues anything: the code (<see cref="EfaturaStatus"/>, 2000 to 2007) and the message the caller reads in the fault <c>EFaturaFault</c>.</summary>
public sealed class EFaturaFaultException(int code, string? message = null) : Exception(message ?? EfaturaStatus.NameOf(code))
{
    public int Code { get; } = code;

    /// <summary>The fault CoreWCF sends: <see cref="EFaturaFaultType"/> with the code and the message.</summary>
    public FaultException<EFaturaFaultType> ToFault() => new(new EFaturaFaultType { Code = Code, Msg = Message }, $"{Code}:{Message}");
}
