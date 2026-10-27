using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos.Filters;
using Edx.Shared.Contracts.Events.Intake;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Services;

public interface IReceivedFileAppService
{
    /// <summary>Keeps what the event says of a file; a file that is kept already is not kept again.</summary>
    Task RecordAsync(FileStoredEto received, CancellationToken cancellationToken = default);

    Task<ReceivedFileDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<ReceivedFileDto>> GetPagedListAsync(GetReceivedFilesPaged pagedInput, CancellationToken cancellationToken = default);
}
