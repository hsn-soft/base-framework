namespace Edx.Mock.Center.MongoDB;

/// <summary>Where the mock center keeps its data: the prefix of its collections and the name of its connection string.</summary>
public static class MongoCenterDbProperties
{
    public const string ConnectionStringName = "Mongo-MockCenter";

    public const string ParticipantCollection = "Participant";

    public const string EnvelopeCollection = "Envelope";

    public static string CollectionPrefix { get; set; } = string.Empty;
}
