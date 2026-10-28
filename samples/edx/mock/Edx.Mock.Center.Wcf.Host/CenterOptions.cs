namespace Edx.Mock.Center.Wcf.Host;

/// <summary>Who the center is and how it works (section <c>Center</c>).</summary>
public sealed class CenterOptions
{
    public const string SectionName = "Center";

    /// <summary>The register number (VKN) of the center: the receiver of the system responses the post boxes send, and the sender of the ones it makes. A participant with this number is made at start.</summary>
    public string RegisterNumber { get; set; } = "9999999999";

    public string Title { get; set; } = "GIB Merkez (mock)";

    /// <summary>The address the center sends its system responses from.</summary>
    public string Alias { get; set; } = "urn:mail:defaultgb@merkez.efatura.mock";

    /// <summary>The most a message may weigh, in bytes (the GİB service says 65 536).</summary>
    public long MaxReceivedMessageSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>The container of the blob store the envelopes are kept in.</summary>
    public string Container { get; set; } = "edx-mock-center";

    /// <summary>How often a worker looks for work, in seconds.</summary>
    public int WorkerIntervalSeconds { get; set; } = 3;

    /// <summary>How many envelopes a worker takes at a time.</summary>
    public int WorkerBatchSize { get; set; } = 50;

    /// <summary>How long to wait before a delivery that failed is tried again, in seconds (the GİB center: two hours).</summary>
    public int ResendWaitSeconds { get; set; } = 7200;

    /// <summary>How many times a delivery that failed is tried again before the center gives up (1215); the GİB center tries four times, two hours apart.</summary>
    public int ResendLimit { get; set; } = 4;

    /// <summary>How long after an envelope was delivered the center asks the post box what became of it (<c>getApplicationResponse</c>), when the post box has not answered by itself (the envelope is still 1220), in seconds.</summary>
    public int AnswerPollAfterSeconds { get; set; } = 3600;

    /// <summary>Whether an invoice and an application response must carry an XML signature (its presence is checked, never whether it is right; 1161).</summary>
    public bool RequireSignature { get; set; } = true;

    /// <summary>The most documents an envelope may hold (all for the same post box: the receiver of the envelope). More is refused by the analysis.</summary>
    public int MaxDocumentsPerEnvelope { get; set; } = 1000;

    /// <summary>
    /// Whether the center sends the sender a <b>system envelope</b> with what its analysis found (and forwards the system envelopes of post boxes). Off, the status of an envelope is read only by asking the center for it (<c>getApplicationResponse</c> with the uuid of the envelope), which is how a sender
    /// that does not wait for a system envelope works; the post box is still asked for its answer to an invoice (<see cref="AnswerPollAfterSeconds"/>).
    /// </summary>
    public bool SendSystemResponses { get; set; }
}

/// <summary>A participant the center is started with (section <c>Participants</c>): the seeder keeps the database in step with the list.</summary>
public sealed class ParticipantSeed
{
    public const string SectionName = "Participants";

    public string RegisterNumber { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<AliasSeed> Aliases { get; set; } = [];
}

public sealed class AliasSeed
{
    public string Name { get; set; } = string.Empty;

    /// <summary><c>SenderUnit</c> or <c>PostBox</c>.</summary>
    public string Type { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;
}
