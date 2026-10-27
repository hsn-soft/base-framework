using HsnSoft.Base;

namespace Edx.IntakeService.Domain.StoredFileDomain.Exceptions;

/// <summary>There is no such file (or it is another tenant's): answered as 404.</summary>
public static class StoredFileNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"File {id} was not found.").WithKey("file_not_found", id);
}
