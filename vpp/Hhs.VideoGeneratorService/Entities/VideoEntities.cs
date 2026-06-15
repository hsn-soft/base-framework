using MongoDB.Bson.Serialization.Attributes;

namespace Hhs.VideoGeneratorService.Entities;

public sealed class VideoRequest
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid? CustomerContentId { get; set; }
    public Guid? AnalysisContentId { get; set; }

    public string ContentProcessType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    public bool ExternalAudioRequired { get; set; }
    public string VideoInputJson { get; set; } = default!;

    public string? VideoProviderRequestId { get; set; }
    public string? ProviderVideoFileUrl { get; set; }
    public string? LocalVideoFilePath { get; set; }
    public string? FinalVideoStorageUrl { get; set; }

    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class AudioRequest
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid VideoRequestId { get; set; }

    public Guid? CustomerContentId { get; set; }
    public Guid? AnalysisContentId { get; set; }

    public int SortOrder { get; set; }

    public string Status { get; set; } = default!;
    public string CurrentStep { get; set; } = default!;

    public string InputText { get; set; } = default!;

    public string? AudioProviderRequestId { get; set; }
    public string? ProviderAudioFileUrl { get; set; }
    public string? LocalAudioFilePath { get; set; }
    public string? AudioStorageUrl { get; set; }

    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; } = 5;
    public DateTime? NextRetryAtUtc { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class VideoGeneratorInboxMessage
{
    [BsonId]
    public Guid EventId { get; set; }

    public string EventName { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime ProcessedAtUtc { get; set; }
}