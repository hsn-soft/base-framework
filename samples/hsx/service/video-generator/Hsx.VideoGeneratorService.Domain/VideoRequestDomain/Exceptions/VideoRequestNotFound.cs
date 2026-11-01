using HsnSoft.Base;

namespace Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Exceptions;

/// <summary>There is no such video request: answered as 404.</summary>
public static class VideoRequestNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"VideoRequest {id} was not found.").WithKey("video_request_not_found", id);
}
