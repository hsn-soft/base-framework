using HsnSoft.Base.MongoDB.Helpers;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class AnalysisContentNormalizedRequestClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisContentNormalizedRequest)))
        {
            BsonClassMap.RegisterClassMap<AnalysisContentNormalizedRequest>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                // Correlation & Tracing
                map.MapMember(x => x.CorrelationId)
                    .SetMaxLength(AnalysisContentNormalizedRequestConsts.CorrelationIdMaxLength);

                // Subscription & Scope
                map.MapMember(x => x.ScopeKey)
                    .SetIsRequired(true)
                    .SetMaxLength(AnalysisContentNormalizedRequestConsts.ScopeKeyMaxLength);

                // Content Reference
                map.MapMember(x => x.DomainName)
                    .SetMaxLength(AnalysisContentNormalizedRequestConsts.DomainNameMaxLength);

                // Status & Progress
                map.MapMember(x => x.Status)
                    .SetIsRequired(true)
                    .SetMaxLength(AnalysisContentNormalizedRequestConsts.StatusMaxLength);
                map.MapMember(x => x.CurrentMilestone)
                    .SetIsRequired(true)
                    .SetMaxLength(AnalysisContentNormalizedRequestConsts.CurrentMilestoneMaxLength);

                // Error Handling
                map.MapMember(x => x.LastError)
                    .SetMaxLength(AnalysisContentNormalizedRequestConsts.LastErrorMaxLength);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnalysisNormalizedItem)))
        {
            BsonClassMap.RegisterClassMap<AnalysisNormalizedItem>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                // Content Reference
                map.MapMember(x => x.ContentKey)
                    .SetIsRequired(true);

                // Scraping State
                map.MapMember(x => x.ScrapingStatus)
                    .SetIsRequired(true);

                // Outline Generation State
                map.MapMember(x => x.OutlineStatus)
                    .SetIsRequired(true);

                // Outline Polling & Tracking
                map.MapMember(x => x.OutlineProviderTrackId);

                // Status & Progress
                map.MapMember(x => x.Status)
                    .SetIsRequired(true);
                map.MapMember(x => x.CurrentMilestone)
                    .SetIsRequired(true);

                // Error Handling & Tracking
                map.MapMember(x => x.LastError);
            });
        }
    }
}