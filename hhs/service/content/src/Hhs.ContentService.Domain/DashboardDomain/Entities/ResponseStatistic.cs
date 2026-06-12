namespace Hhs.ContentService.Domain.DashboardDomain.Entities;

// public sealed class ResponseStatistic : Entity<Guid>, IMultiTenant
// {
//     public Guid TenantId { get; private set; }
//
//     public Guid ClientId { get; private set; }
//
//     [NotNull]
//     public string ResponseStatus { get; private set; }
//
//     public ulong ResponseTime { get; internal set; }
//
//     public ulong ResponseCount { get; internal set; }
//
//     private ResponseStatistic()
//     {
//         ResponseStatus = string.Empty;
//     }
//
//     internal ResponseStatistic(Guid id, Guid tenantId, Guid clientId, [NotNull] string responseStatus, ulong responseTime, ulong responseCount) : this()
//     {
//         Id = id;
//         SetTenantId(tenantId);
//         SetClientId(clientId);
//         SetResponseStatus(responseStatus);
//
//         ResponseTime = responseTime;
//         ResponseCount = responseCount;
//     }
//
//     internal void SetTenantId(Guid tenantId)
//     {
//         if (tenantId == Guid.Empty)
//         {
//             throw new ArgumentException($"{nameof(TenantId)} is invalid", nameof(tenantId));
//         }
//
//         TenantId = tenantId;
//     }
//
//     internal void SetClientId(Guid clientId)
//     {
//         if (clientId == Guid.Empty)
//         {
//             throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
//         }
//
//         ClientId = clientId;
//     }
//
//     internal void SetResponseStatus(string responseStatus)
//     {
//         string result = Check.NotNull(responseStatus, nameof(responseStatus), ResponseStatisticConsts.ResponseStatusMaxLength);
//
//         switch (result)
//         {
//             case AppContentPublicStatus.READY: { break; }
//             case AppContentPublicStatus.CREATED: { break; }
//             case AppContentPublicStatus.NO_ANALYSIS_VIDEO: { break; }
//             case AppContentPublicStatus.SKIPPED_PATH: { break; }
//             default: throw new ArgumentException($"{nameof(ResponseStatus)} is invalid", nameof(responseStatus));
//         }
//
//         ResponseStatus = result.ToLower(new CultureInfo("en-US"));
//     }
// }