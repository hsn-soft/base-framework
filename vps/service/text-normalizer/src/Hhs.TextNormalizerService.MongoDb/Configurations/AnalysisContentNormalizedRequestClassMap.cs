using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class AnalysisContentNormalizedRequestClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<AnalysisContentNormalizedRequest>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            // Correlation & Tracing - MaxLength: AnalysisContentNormalizedRequestConsts.CorrelationIdMaxLength
            // Subscription & Scope - MaxLength: AnalysisContentNormalizedRequestConsts.ScopeKeyMaxLength
            map.MapMember(x => x.ScopeKey).SetIsRequired(true);

            // Content Reference - MaxLength: AnalysisContentNormalizedRequestConsts.DomainNameMaxLength

            // Status & Progress - MaxLength: AnalysisContentNormalizedRequestConsts.StatusMaxLength, AnalysisContentNormalizedRequestConsts.CurrentStepMaxLength
            map.MapMember(x => x.Status).SetIsRequired(true);
            map.MapMember(x => x.CurrentStep).SetIsRequired(true);

            // Error Handling - MaxLength: AnalysisContentNormalizedRequestConsts.LastErrorMaxLength
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisNormalizedItem)))
        {
            BsonClassMap.RegisterClassMap<AnalysisNormalizedItem>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                // Content Reference - MaxLength: AnalysisNormalizedItemConsts.ContentKeyMaxLength
                map.MapMember(x => x.ContentKey).SetIsRequired(true);

                // Scraping State - MaxLength: AnalysisNormalizedItemConsts.ScrapingStatusMaxLength
                map.MapMember(x => x.ScrapingStatus).SetIsRequired(true);

                // Outline Generation State - MaxLength: AnalysisNormalizedItemConsts.OutlineStatusMaxLength
                map.MapMember(x => x.OutlineStatus).SetIsRequired(true);

                // Outline Polling & Tracking - MaxLength: AnalysisNormalizedItemConsts.OutlineProviderTrackIdMaxLength

                // Status & Progress - MaxLength: AnalysisNormalizedItemConsts.StatusMaxLength, AnalysisNormalizedItemConsts.CurrentStepMaxLength
                map.MapMember(x => x.Status).SetIsRequired(true);
                map.MapMember(x => x.CurrentStep).SetIsRequired(true);

                // Error Handling & Tracking - MaxLength: AnalysisNormalizedItemConsts.LastErrorMaxLength
            });
        }
    }
}