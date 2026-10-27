using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Filters;

public sealed class GetStoredFilesPaged : PagedDataRequestDto
{
    /// <summary>A part of the file name.</summary>
    [StringLength(100)]
    public string? FileName { get; set; }

    /// <summary><c>XmlArchive</c>, <c>TabularArchive</c>, <c>ModelJson</c> or <c>ModelXml</c>; all when it is not given.</summary>
    [StringLength(20)]
    public string? Kind { get; set; }

    /// <summary><c>Rest</c>, <c>Grpc</c> or <c>Wcf</c>; all when it is not given.</summary>
    [StringLength(10)]
    public string? Channel { get; set; }
}
