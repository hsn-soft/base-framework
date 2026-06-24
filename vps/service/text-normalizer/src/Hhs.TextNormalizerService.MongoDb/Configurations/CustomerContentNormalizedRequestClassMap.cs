using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class CustomerContentNormalizedRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<CustomerContentNormalizedRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            // Correlation & Tracing - MaxLength: CustomerContentNormalizedRequestConsts.CorrelationIdMaxLength
            // Subscription & Scope - MaxLength: CustomerContentNormalizedRequestConsts.ScopeKeyMaxLength
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);

            // Content Reference - MaxLength: CustomerContentNormalizedRequestConsts.DomainNameMaxLength, ContentKeyMaxLength
            map.MapMember(x => x.DomainName).SetIsRequired(true);
            map.MapMember(x => x.ContentKey).SetIsRequired(true);

            // Status & Progress - MaxLength: CustomerContentNormalizedRequestConsts.StatusMaxLength, CurrentStepMaxLength
            map.MapMember(x => x.Status).SetIsRequired(true);
            map.MapMember(x => x.CurrentStep).SetIsRequired(true);

            // Scraping State - MaxLength: CustomerContentNormalizedRequestConsts.ScrapingStatusMaxLength

            // Outline Generation State - MaxLength: CustomerContentNormalizedRequestConsts.OutlineStatusMaxLength, OutlineProviderTrackIdMaxLength

            // Error Handling - MaxLength: CustomerContentNormalizedRequestConsts.LastErrorMaxLength
        });
    }
}