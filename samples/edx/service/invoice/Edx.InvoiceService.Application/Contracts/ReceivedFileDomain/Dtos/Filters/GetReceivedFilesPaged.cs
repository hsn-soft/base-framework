using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos.Filters;

public sealed class GetReceivedFilesPaged : PagedDataRequestDto
{
    /// <summary>A part of the file name.</summary>
    [StringLength(100)]
    public string? FileName { get; set; }

    /// <summary>What the file is (<c>XmlArchive</c>, <c>TabularArchive</c>, <c>ModelJson</c>, <c>ModelXml</c>); all when it is not given.</summary>
    [StringLength(20)]
    public string? Kind { get; set; }
}
