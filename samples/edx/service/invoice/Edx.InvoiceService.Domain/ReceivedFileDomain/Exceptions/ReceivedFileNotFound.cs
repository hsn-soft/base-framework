using HsnSoft.Base;

namespace Edx.InvoiceService.Domain.ReceivedFileDomain.Exceptions;

/// <summary>There is no such record (or it is another tenant's): answered as 404.</summary>
public static class ReceivedFileNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Received file {id} was not found.").WithKey("received_file_not_found", id);
}
