using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Filters;

public sealed class GetQueueItemsPaged : PagedDataRequestDto
{
    /// <summary>A part of the invoice number.</summary>
    [StringLength(64)]
    public string? InvoiceNumber { get; set; }

    /// <summary><c>Queued</c>, <c>DraftCreated</c>, <c>Rejected</c>, <c>Failed</c> or <c>Completed</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? Status { get; set; }

    /// <summary><c>Model</c>, <c>Xml</c>, <c>Txt</c>, <c>Csv</c> or <c>Xls</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? SourceType { get; set; }

    /// <summary><c>ServiceDto</c>, <c>ServiceUploadFile</c>, <c>BackOfficeCreatePage</c> or <c>BackOfficeUploadPage</c>; all when it is not given.</summary>
    [StringLength(24)]
    public string? ChannelType { get; set; }
}
