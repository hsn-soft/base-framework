using HsnSoft.Base.Application.Dtos;

namespace Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;

/// <summary>What the service says of a file it received.</summary>
public sealed class StoredFileDto : CreationAuditedEntityDto<Guid>
{
    /// <summary><c>XmlArchive</c>, <c>TabularArchive</c>, <c>ModelJson</c> or <c>ModelXml</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary><c>Rest</c>, <c>Grpc</c> or <c>Wcf</c>.</summary>
    public string Channel { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public string FolderPath { get; init; } = string.Empty;

    public string Container { get; init; } = string.Empty;

    public string ObjectName { get; init; } = string.Empty;

    public IReadOnlyList<StoredFileEntryDto> Entries { get; init; } = [];

    public Guid TenantId { get; init; }
}

/// <summary>A file inside a received archive.</summary>
public sealed class StoredFileEntryDto
{
    public string Name { get; init; } = string.Empty;

    public long SizeBytes { get; init; }
}
