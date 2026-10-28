using System.Text;
using CoreWCF;
using CoreWCF.Channels;

namespace Edx.Shared.Efatura.Server;

/// <summary>The binding the center serves with: the one of the GİB service (<c>BasicHttpBinding</c>, MTOM, buffered, UTF-8, ten minutes to receive).</summary>
public static class EFaturaServerBinding
{
    /// <param name="useTransportSecurity">Whether it is HTTPS.</param>
    /// <param name="maxReceivedMessageSize">The most a message may weigh, in bytes (the GİB service says 65 536).</param>
    public static BasicHttpBinding Create(bool useTransportSecurity, long maxReceivedMessageSize)
    {
        return new BasicHttpBinding(useTransportSecurity ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None)
        {
            Name = useTransportSecurity ? "EFaturaHttpsServiceBinding" : "EFaturaHttpServiceBinding",
            CloseTimeout = TimeSpan.FromMinutes(1),
            OpenTimeout = TimeSpan.FromMinutes(1),
            ReceiveTimeout = TimeSpan.FromMinutes(10),
            SendTimeout = TimeSpan.FromMinutes(10),
            MessageEncoding = WSMessageEncoding.Mtom,
            MaxReceivedMessageSize = maxReceivedMessageSize,
            MaxBufferSize = (int)Math.Min(maxReceivedMessageSize, int.MaxValue),
            TextEncoding = Encoding.UTF8,
            TransferMode = TransferMode.Buffered,
        };
    }
}
