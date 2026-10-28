namespace Edx.Integration.GibInvoice.Application.Options;

/// <summary>The center the service exchanges envelopes with, and how it works with it (section <c>Center</c>).</summary>
public sealed class GibCenterOptions
{
    public const string SectionName = "Center";

    /// <summary>The service of the center (<c>sendDocument</c>, <c>getApplicationResponse</c>).</summary>
    public string Endpoint { get; set; } = "http://localhost:5700/EFaturaMerkez/services/EFatura/";

    /// <summary>Where the list of the registered users (the post boxes) is.</summary>
    public string UserListUrl { get; set; } = "http://localhost:5700/newUserPkListxml_full.zip";

    /// <summary>The register number of the center: a system response that comes from it is the center's word; one that comes from another is a post box's.</summary>
    public string RegisterNumber { get; set; } = "9999999999";

    /// <summary>The most a message may weigh, in bytes.</summary>
    public long MaxMessageSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>How long to wait before an envelope that could not be handed over is tried again, in seconds.</summary>
    public int SendRetryWaitSeconds { get; set; } = 60;

    /// <summary>How many times an envelope that could not be handed over is tried again before it fails.</summary>
    public int SendRetryLimit { get; set; } = 5;

    /// <summary>How long after the center took an envelope, or after the last news, the service asks the center what became of it (<c>getApplicationResponse</c>), in seconds.</summary>
    public int StatusPollAfterSeconds { get; set; } = 300;

    /// <summary>How often the list of the registered users is read again, in minutes (the guides say: at most once an hour).</summary>
    public int UserListRefreshMinutes { get; set; } = 60;

    /// <summary>Whether an invoice that comes in must carry an XML signature (its presence is checked, never whether it is right; 1161).</summary>
    public bool RequireSignature { get; set; } = true;

    /// <summary>
    /// Whether the post box answers an invoice envelope with a <b>system envelope</b> sent to the center. Off (the sample's choice, to keep it simple), the post box answers only when it is asked (<c>getApplicationResponse</c>), and the status of an envelope the sender unit sent is read by asking the center
    /// for it with the uuid of the envelope; a system envelope that does come is still applied.
    /// </summary>
    public bool SendSystemResponses { get; set; }

    /// <summary>How often a worker looks for work, in seconds.</summary>
    public int WorkerIntervalSeconds { get; set; } = 3;

    /// <summary>How many envelopes a worker takes at a time.</summary>
    public int WorkerBatchSize { get; set; } = 50;
}
