using System.ComponentModel.DataAnnotations;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.TenantDomain.Repositories;

public interface IAppContentRepository : IReadOnlyGenericRepository<AppContent, Guid>
{
    Task<PagedQueryResult<AppContentListDto>> GetAccessiblePagedListAsync(
        AppContentPagedInput input,
        CancellationToken cancellationToken = default);
}

public sealed class AppContentListDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ClientId { get; set; }

    public string ClientDomain { get; set; }

    public Guid ProductTypeId { get; set; }

    public string ProductTypeCode { get; set; }

    public string SlugKey { get; set; }

    public AppContentOperationStates OperationStatus { get; set; }

    public DateTime CreationTime { get; set; }
}

public sealed class AppContentPagedInput
{
    public Guid? ClientId { get; set; }

    public Guid? ProductTypeId { get; set; }

    public string SlugKey { get; set; }

    public AppContentOperationStates? OperationStatus { get; set; }

    public int PageNumber { get; set; } = 1;

    public int MaxResultCount { get; set; } = 20;
}

public enum AppContentOperationStates
{
    [Display(Name = "CreatedWaitForNormalize")]
    CreatedWaitForNormalize = 0,

    [Display(Name = "NormalizedWaitForVideoGenerationApprove")]
    NormalizedWaitForVideoGenerationApprove = 11,

    [Display(Name = "VideoGenerationApprovedWaitForGenerationResults")]
    VideoGenerationApprovedWaitForGenerationResults = 21,

    [Display(Name = "Fail")]
    OperationFail = 51,

    [Display(Name = "VideoGenerationRejectedReturnAnalysisVideo")]
    VideoGenerationRejectedReturnAnalysisVideo = 91,

    [Display(Name = "Success")]
    OperationSuccess = 99
}