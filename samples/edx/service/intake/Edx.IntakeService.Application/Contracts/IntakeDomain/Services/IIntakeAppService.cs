using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Filters;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using HsnSoft.Base.Application.Dtos;

namespace Edx.IntakeService.Application.Contracts.IntakeDomain.Services;

/// <summary>
/// What the service does, whatever the way a customer reaches it: REST, gRPC and WCF all call this, and only the Host knows which. Every file received is kept in the blob store, recorded (with its folders) in the
/// database and said to the rest of the system by an event.
/// </summary>
public interface IIntakeAppService
{
    /// <summary>Receives a zip of XML files (each well formed).</summary>
    Task<StoredFileDto> ReceiveXmlArchiveAsync(string fileName, Stream content, IntakeChannel channel, CancellationToken cancellationToken = default);

    /// <summary>Receives a zip of text, CSV or Excel files.</summary>
    Task<StoredFileDto> ReceiveTabularArchiveAsync(string fileName, Stream content, IntakeChannel channel, CancellationToken cancellationToken = default);

    /// <summary>Receives a model of the shared API and keeps it as a JSON or an XML file.</summary>
    Task<StoredFileDto> ReceiveInvoiceModelAsync(InvoiceSubmissionDto model, ModelFormat format, IntakeChannel channel, CancellationToken cancellationToken = default);

    Task<StoredFileDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<StoredFileDto>> GetPagedListAsync(GetStoredFilesPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>The bytes of a file as they were received, with its name and content type.</summary>
    Task<StoredFileContent> OpenAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>A file read back from the store: the caller disposes the stream.</summary>
public sealed record StoredFileContent(Stream Content, string FileName, string ContentType);
