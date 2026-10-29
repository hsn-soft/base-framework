namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Consts;

public static class EnvelopeConsts
{
    public const string OutboundCollectionName = "OutboundEnvelope";
    public const string InboundCollectionName = "InboundEnvelope";

    public const int ReasonMaxLength = 2000;
    public const int PathMaxLength = 512;

    /// <summary>How many times the processing of an envelope that came in may fail on something that is not the envelope's fault (the store, the database) before it is answered with 1195.</summary>
    public const int MaxProcessAttempts = 5;
}
