using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Text;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class AppContent : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public Guid ClientId { get; private set; }

    public ClientNew Client { get; private set; } = null!;

    public Guid ProductTypeId { get; private set; }

    public ProductType ProductType { get; private set; } = null!;

    public string SlugKey { get; private set; } = string.Empty;

    public AppContentOperationStates OperationStatus { get; set; }

    public string OperationStatusDescription { get; set; }

    public Guid? NormalizedRequestId { get; set; }

    public DateTime? ReleaseTime { get; private set; }

    public Guid? VideoRequestId { get; set; }

    public string StorageVideoUrl { get; set; }

    public string CorrelationId { get; set; }

    private AppContent()
    {
    }

    internal AppContent(
        Guid clientId,
        Guid productTypeId,
        string slugKey,
        AppContentOperationStates operationStatus,
        string correlationId = null)
    {
        Id = Guid.CreateVersion7();
        ClientId = clientId;
        ProductTypeId = productTypeId;
        SetSlugKey(slugKey);
        OperationStatus = operationStatus;
        CorrelationId = correlationId;
    }

    internal void SetSlugKey(string slugKey)
    {
        string checkSlugKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            slugKey,
            $"{nameof(AppContent)}:{nameof(SlugKey)}",
            AppContentConsts.SlugKeyMaxLength);

        SlugKey = StringHelper.SlugKeyNormalize(checkSlugKey);
    }
}