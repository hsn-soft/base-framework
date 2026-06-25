using HsnSoft.Base.MongoDB.Helpers;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Consts;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.TextNormalizerService.MongoDb.Configurations;

public static class CustomerContentNormalizedRequestClassMap
{
    public static void Register()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(CustomerContentNormalizedRequest)))
        {
            BsonClassMap.RegisterClassMap<CustomerContentNormalizedRequest>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);

                // Correlation & Tracing
                map.MapMember(x => x.CorrelationId)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.CorrelationIdMaxLength);

                // Subscription & Scope
                map.MapMember(x => x.ScopeKey)
                    .SetIsRequired(true)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.ScopeKeyMaxLength);

                // Content Reference
                map.MapMember(x => x.DomainName)
                    .SetIsRequired(true)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.DomainNameMaxLength);
                map.MapMember(x => x.ContentKey)
                    .SetIsRequired(true)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.ContentKeyMaxLength);

                // Status & Progress
                map.MapMember(x => x.Status)
                    .SetIsRequired(true)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.StatusMaxLength);
                map.MapMember(x => x.CurrentStep)
                    .SetIsRequired(true)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.CurrentStepMaxLength);

                // Scraping State
                map.MapMember(x => x.ScrapingStatus)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.ScrapingStatusMaxLength);

                // Outline Generation State
                map.MapMember(x => x.OutlineStatus)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.OutlineStatusMaxLength);
                map.MapMember(x => x.OutlineProviderTrackId)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.OutlineProviderTrackIdMaxLength);

                // Error Handling
                map.MapMember(x => x.LastError)
                    .SetMaxLength(CustomerContentNormalizedRequestConsts.LastErrorMaxLength);
            });
        }
    }
}