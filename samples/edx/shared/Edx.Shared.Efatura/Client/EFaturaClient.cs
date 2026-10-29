using System.ServiceModel;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Wire;

namespace Edx.Shared.Efatura.Client;

/// <summary>What a call to the center (or to a post box) came to.</summary>
/// <param name="Accepted">The other side took the envelope (or answered the question).</param>
/// <param name="Code">The code of the fault when the other side refused (<c>0</c> when it did not).</param>
/// <param name="Message">The message of the other side, or what went wrong on the way.</param>
/// <param name="CanRetry">Whether it is worth trying again: the other side could not be reached (not: it said no).</param>
/// <param name="ApplicationResponse">The answer of <c>getApplicationResponse</c>: the system envelope XML.</param>
public sealed record EFaturaResult(bool Accepted, int Code, string? Message, bool CanRetry, string? ApplicationResponse = null);

/// <summary>
/// Calls the center or a post box: <c>sendDocument</c> (an envelope: the uuid-named zip and its MD5, as an MTOM attachment) and <c>getApplicationResponse</c>. A refusal of the other side is a result (<see cref="EFaturaResult.Code"/>), and so is a failure to reach it
/// (<see cref="EFaturaResult.CanRetry"/>); nothing is thrown for either.
/// </summary>
public sealed class EFaturaClient : IDisposable
{
    private readonly ChannelFactory<IEFaturaClientPort> _factory;

    /// <param name="endpoint">The address of the service (<c>http://host:port/EFaturaMerkez/services/EFatura/</c>).</param>
    /// <param name="maxMessageSize">The most a message may weigh, in bytes.</param>
    public EFaturaClient(Uri endpoint, long maxMessageSize = 10 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        BasicHttpBinding binding = new(endpoint.Scheme == Uri.UriSchemeHttps ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None)
        {
            Name = "GibBasicHttpBinding",
            MaxReceivedMessageSize = maxMessageSize,
            MaxBufferSize = (int)Math.Min(maxMessageSize, int.MaxValue),
            ReaderQuotas = { MaxStringContentLength = (int)Math.Min(maxMessageSize, int.MaxValue) },
            SendTimeout = TimeSpan.FromMinutes(2),
            OpenTimeout = TimeSpan.FromSeconds(30),
            ReceiveTimeout = TimeSpan.FromMinutes(2),
            CloseTimeout = TimeSpan.FromSeconds(30),

            // MTOM must be on: the zip travels as an attachment
            MessageEncoding = WSMessageEncoding.Mtom,
        };
        _factory = new ChannelFactory<IEFaturaClientPort>(binding, new EndpointAddress(endpoint));
    }

    /// <summary>Sends the envelope: the zip is named <c>{envelopeId}.zip</c> and goes with its MD5.</summary>
    public async Task<EFaturaResult> SendAsync(string envelopeId, byte[] zip)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeId);
        ArgumentNullException.ThrowIfNull(zip);

        IEFaturaClientPort channel = _factory.CreateChannel();
        try
        {
            ClientSendDocumentReply reply = await channel.SendDocumentAsync(new ClientSendDocumentCall
            {
                Document = new DocumentType
                {
                    FileName = envelopeId + ".zip",
                    BinaryData = new Base64Binary { ContentType = "text/xml; charset=utf-8", Value = zip },
                    Hash = Hashing.Md5Hex(zip),
                },
            }).ConfigureAwait(false);
            Close(channel, success: true);
            return reply.Response is null ? new EFaturaResult(false, 0, "The answer is empty.", CanRetry: false) : new EFaturaResult(true, 0, reply.Response.Msg, CanRetry: false);
        }
        catch (FaultException<EFaturaFaultType> fault)
        {
            Close(channel, success: false);
            return new EFaturaResult(false, fault.Detail.Code, fault.Detail.Msg, CanRetry: false);
        }
        catch (Exception exception) when (exception is CommunicationException or TimeoutException or HttpRequestException)
        {
            Close(channel, success: false);
            return new EFaturaResult(false, 0, exception.Message, CanRetry: true);
        }
    }

    /// <summary>Asks the center what became of the envelope; the answer is the XML of a system envelope.</summary>
    public async Task<EFaturaResult> QueryAsync(string envelopeId)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeId);

        IEFaturaClientPort channel = _factory.CreateChannel();
        try
        {
            ClientGetApplicationResponseReply reply = await channel.GetApplicationResponseAsync(new ClientGetApplicationResponseCall
            {
                Request = new GetAppRespRequestType { InstanceIdentifier = envelopeId },
            }).ConfigureAwait(false);
            Close(channel, success: true);
            return new EFaturaResult(true, 0, null, CanRetry: false, reply.Response?.ApplicationResponse);
        }
        catch (FaultException<EFaturaFaultType> fault)
        {
            Close(channel, success: false);
            return new EFaturaResult(false, fault.Detail.Code, fault.Detail.Msg, CanRetry: false);
        }
        catch (Exception exception) when (exception is CommunicationException or TimeoutException or HttpRequestException)
        {
            Close(channel, success: false);
            return new EFaturaResult(false, 0, exception.Message, CanRetry: true);
        }
    }

    public void Dispose()
    {
        if (_factory.State == CommunicationState.Faulted)
        {
            _factory.Abort();
        }
        else
        {
            _factory.Close();
        }
    }

    private static void Close(IEFaturaClientPort channel, bool success)
    {
        IClientChannel client = (IClientChannel)channel;
        if (success)
        {
            client.Close();
        }
        else
        {
            client.Abort();
        }

        client.Dispose();
    }
}
