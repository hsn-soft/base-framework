using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;

public sealed class VideoRequest : CreationAuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public ReferenceContentTypes RefContentType { get; private set; }
    public Guid RefContentId { get; private set; }

    public VideoRequestStates OperationStatus { get; internal set; }

    [CanBeNull] public string OperationStatusDescription { get; internal set; }

    [NotNull] public List<NormalizedContentData> NormalizedContentDatas { get; private set; }

    [CanBeNull] public List<string> AudioFileNames { get; internal set; }

    [CanBeNull] public string ExternalVideoTraceId { get; private set; }

    public int QueryCount { get; set; }

    public DateTime LastQueryTime { get; private set; }

    [CanBeNull] public string ExternalVideoUrl { get; internal set; }

    [CanBeNull] public string LocalVideoPath { get; internal set; }

    [CanBeNull] public string StorageVideoTraceId { get; private set; }

    [CanBeNull] public string StorageVideoUrl { get; internal set; }

    [CanBeNull] public string CorrelationId { get; internal set; }


    private VideoRequest()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        DomainName = string.Empty;
    }

    internal VideoRequest(Guid id, [NotNull] string scopeKey, [NotNull] string domainName, ReferenceContentTypes refContentType, Guid refContentId, VideoRequestStates operationStatus, [NotNull] List<NormalizedContentData> normalizedContentDatas,
        [CanBeNull] string operationStatusDescription = null, [CanBeNull] List<string> audioFileNames = null, [CanBeNull] string externalVideoTraceId = null, [CanBeNull] string externalVideoUrl = null,
        [CanBeNull] string storageVideoTraceId = null, [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(scopeKey);
        SetDomainName(domainName);
        SetRefContent(refContentType, refContentId);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetNormalizedContentDatas(normalizedContentDatas);
        AudioFileNames = audioFileNames;
        SetExternalVideoTraceId(externalVideoTraceId);
        QueryCount = 0;
        SetLastQueryTime(new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0)); // Set default value
        ExternalVideoUrl = externalVideoUrl;
        SetStorageVideoTraceId(storageVideoTraceId);
        StorageVideoUrl = storageVideoUrl;
        CorrelationId = correlationId;
    }


    private void SetScopeKey(string scopeKey)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            scopeKey,
            $"{nameof(VideoRequest)}:{nameof(ScopeKey)}",
            VideoRequestConsts.ScopeKeyMaxLength
        );

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(VideoRequest)}:{nameof(DomainName)}", VideoRequestConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }

    internal void SetRefContent(ReferenceContentTypes refContentType, Guid refContentId)
    {
        if (refContentId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(RefContentId)} is invalid", nameof(refContentId));
        }

        RefContentType = refContentType;
        RefContentId = refContentId;
    }

    internal void SetNormalizedContentDatas(List<NormalizedContentData> normalizedContentDatas)
    {
        if (normalizedContentDatas is { Count: > 0 })
        {
            foreach (var normalizedContentData in normalizedContentDatas)
            {
                Check.NotNullOrWhiteSpace(normalizedContentData.NormalizedContent, nameof(normalizedContentData.NormalizedContent));
            }

            NormalizedContentDatas = normalizedContentDatas;
        }
        else
        {
            throw new ArgumentException($"{nameof(NormalizedContentDatas)} is not empty", nameof(normalizedContentDatas));
        }
    }

    internal void SetLastQueryTime(DateTime lastQueryTime)
    {
        if (lastQueryTime > DateTime.Now || lastQueryTime < DateTime.Now.AddYears(-10))
        {
            throw new ArgumentException($"{nameof(LastQueryTime)} is invalid", nameof(lastQueryTime));
        }

        LastQueryTime = lastQueryTime.ToUniversalTime();
    }

    internal void SetExternalVideoTraceId(string externalVideoTraceId)
    {
        ExternalVideoTraceId = string.IsNullOrWhiteSpace(externalVideoTraceId)
            ? null
            : Check.Length(externalVideoTraceId, nameof(externalVideoTraceId), VideoRequestConsts.ExternalVideoTraceIdMaxLength);
    }

    internal void SetStorageVideoTraceId(string storageVideoTraceId)
    {
        StorageVideoTraceId = string.IsNullOrWhiteSpace(storageVideoTraceId)
            ? null
            : Check.Length(storageVideoTraceId, nameof(storageVideoTraceId), VideoRequestConsts.StorageVideoTraceIdMaxLength);
    }
}

public sealed class NormalizedContentData
{
    public string TitleText { get; set; }

    [NotNull] public string NormalizedContent { get; set; }

    public string ImageUrl { get; set; }
}