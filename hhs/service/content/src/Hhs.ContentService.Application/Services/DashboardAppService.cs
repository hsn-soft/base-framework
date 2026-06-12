namespace Hhs.ContentService.Application.Services;

// public sealed class DashboardAppService : ApplicationServiceBase, IDashboardAppService
// {
//     private readonly IAppConsoleLogger _logger;
//     private readonly IResponseStatisticRepository _responseStatisticRepository;
//     private readonly IAppContentVisitRepository _appContentVisitRepository;
//     private readonly ICustomerContentSettingRepository _clientRepository;
//
//     public DashboardAppService(IServiceProvider provider,
//         IResponseStatisticRepository responseStatisticRepository,
//         IAppContentVisitRepository appContentVisitRepository,
//         ICustomerContentSettingRepository clientRepository
//     ) : base(provider)
//     {
//         _logger = provider.GetRequiredService<IAppConsoleLogger>();
//         _responseStatisticRepository = responseStatisticRepository;
//         _appContentVisitRepository = appContentVisitRepository;
//         _clientRepository = clientRepository;
//     }
//
//     public async Task<DailyResponsesTotalsDto> GetDailyResponsesTotalsAsync(CancellationToken cancellationToken = default)
//     {
//         var currentDate = DateTime.UtcNow.Date;
//         double startTime = currentDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//         double endTime = currentDate.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//
//         var dataList = await _responseStatisticRepository.GetListAsync(new ListQueryOptions<ResponseStatistic> { Filter = x => !string.IsNullOrWhiteSpace(x.ResponseStatus) && x.ResponseTime >= startTime && x.ResponseTime < endTime },
//             cancellationToken: cancellationToken);
//         if (dataList is { Count: > 0 })
//         {
//             return new DailyResponsesTotalsDto
//             {
//                 Labels = [L[AppContentPublicStatus.CREATED].ToString(), L[AppContentPublicStatus.READY].ToString(), L[AppContentPublicStatus.SKIPPED_PATH].ToString(), L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString()],
//                 Series =
//                 [
//                     dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.CREATED.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                     dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.READY.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                     dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.SKIPPED_PATH.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                     dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.NO_ANALYSIS_VIDEO.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount)
//                 ]
//             };
//         }
//
//         return new DailyResponsesTotalsDto
//         {
//             Labels = [L[AppContentPublicStatus.CREATED].ToString(), L[AppContentPublicStatus.READY].ToString(), L[AppContentPublicStatus.SKIPPED_PATH].ToString(), L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString()],
//             Series = [0, 0, 0, 0]
//         };
//     }
//
//     public async Task<WeeklyResponsesAnalysisDto> GetWeeklyResponsesAnalysisAsync(CancellationToken cancellationToken = default)
//     {
//         var currentDate = DateTime.UtcNow.Date;
//         double startTime = currentDate.AddDays(-6).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//         double endTime = currentDate.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//
//         var dataList = await _responseStatisticRepository.GetListAsync(new ListQueryOptions<ResponseStatistic> { Filter = x => !string.IsNullOrWhiteSpace(x.ResponseStatus) && x.ResponseTime >= startTime && x.ResponseTime < endTime },
//             cancellationToken: cancellationToken);
//         if (dataList is { Count: > 0 })
//         {
//             var categoryList = new List<string>();
//             var createdList = new List<int>();
//             var readyList = new List<int>();
//             var skippedList = new List<int>();
//             var novideoList = new List<int>();
//
//             for (int day = 0; day < 7; day++)
//             {
//                 var loopDate = currentDate.AddDays(-6 + day);
//                 categoryList.Add(loopDate.Day.ToString().PadLeft(2, '0'));
//
//                 double startLoopTime = loopDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//                 double endLoopTime = loopDate.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//
//                 createdList.Add(
//                     dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.CREATED.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime).Sum(x => (int)x.ResponseCount));
//                 readyList.Add(dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.READY.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime).Sum(x => (int)x.ResponseCount));
//                 skippedList.Add(dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.SKIPPED_PATH.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime)
//                     .Sum(x => (int)x.ResponseCount));
//                 novideoList.Add(dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.NO_ANALYSIS_VIDEO.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime)
//                     .Sum(x => (int)x.ResponseCount));
//             }
//
//             return new WeeklyResponsesAnalysisDto
//             {
//                 Categories = categoryList.ToArray(),
//                 Series =
//                 [
//                     new() { Name = L[AppContentPublicStatus.CREATED].ToString(), Data = createdList.ToArray() }, new() { Name = L[AppContentPublicStatus.READY].ToString(), Data = readyList.ToArray() },
//                     new() { Name = L[AppContentPublicStatus.SKIPPED_PATH].ToString(), Data = skippedList.ToArray() }, new() { Name = L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString(), Data = novideoList.ToArray() }
//                 ]
//             };
//         }
//
//         var categoryTempList = new List<string>();
//         var zeroDataList = new List<int>();
//         for (int day = 0; day < 7; day++)
//         {
//             var loopDate = currentDate.AddDays(-6 + day);
//             categoryTempList.Add(loopDate.Day.ToString().PadLeft(2, '0'));
//
//             zeroDataList.Add(0);
//         }
//
//         return new WeeklyResponsesAnalysisDto
//         {
//             Categories = categoryTempList.ToArray(),
//             Series =
//             [
//                 new() { Name = L[AppContentPublicStatus.CREATED].ToString(), Data = zeroDataList.ToArray() }, new() { Name = L[AppContentPublicStatus.READY].ToString(), Data = zeroDataList.ToArray() },
//                 new() { Name = L[AppContentPublicStatus.SKIPPED_PATH].ToString(), Data = zeroDataList.ToArray() }, new() { Name = L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString(), Data = zeroDataList.ToArray() }
//             ]
//         };
//     }
//
//     public async Task<MonthlyResponsesTotalsDto> GetMonthlyResponsesTotalsAsync(CancellationToken cancellationToken = default)
//     {
//         var currentDate = DateTime.UtcNow.Date;
//         var currentMonth = new DateTime(currentDate.Year, currentDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
//         double startTime = currentMonth.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//         double endTime = currentMonth.AddMonths(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//
//         var dataList = await _responseStatisticRepository.GetListAsync(new ListQueryOptions<ResponseStatistic> { Filter = x => !string.IsNullOrWhiteSpace(x.ResponseStatus) && x.ResponseTime >= startTime && x.ResponseTime < endTime },
//             cancellationToken: cancellationToken);
//         if (dataList is { Count: > 0 })
//         {
//             return new MonthlyResponsesTotalsDto
//             {
//                 Series =
//                 [
//                     new()
//                     {
//                         Name = L[AppContentPublicStatus.CREATED].ToString(),
//                         Count = dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.CREATED.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                         Border = "primary"
//                     },
//                     new()
//                     {
//                         Name = L[AppContentPublicStatus.READY].ToString(),
//                         Count = dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.READY.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                         Border = "success"
//                     },
//                     new()
//                     {
//                         Name = L[AppContentPublicStatus.SKIPPED_PATH].ToString(),
//                         Count = dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.SKIPPED_PATH.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                         Border = "info"
//                     },
//                     new()
//                     {
//                         Name = L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString(),
//                         Count = dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.NO_ANALYSIS_VIDEO.ToLower(new CultureInfo("en-US")))).Sum(x => (int)x.ResponseCount),
//                         Border = "warning"
//                     }
//                 ]
//             };
//         }
//
//         return new MonthlyResponsesTotalsDto
//         {
//             Series =
//             [
//                 new() { Name = L[AppContentPublicStatus.CREATED].ToString(), Count = 0, Border = "primary" }, new() { Name = L[AppContentPublicStatus.READY].ToString(), Count = 0, Border = "success" },
//                 new() { Name = L[AppContentPublicStatus.SKIPPED_PATH].ToString(), Count = 0, Border = "info" }, new() { Name = L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString(), Count = 0, Border = "warning" }
//             ]
//         };
//     }
//
//     public async Task<MonthlyResponsesAnalysisDto> GetMonthlyResponsesAnalysisAsync(CancellationToken cancellationToken = default)
//     {
//         var currentDate = DateTime.UtcNow.Date;
//         var dateStart = new DateTime(currentDate.Year, currentDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
//         double startTime = dateStart.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//         double endTime = dateStart.AddMonths(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//         var dateEnd = dateStart.AddMonths(1).AddDays(-1);
//
//         var dataList = await _responseStatisticRepository.GetListAsync(new ListQueryOptions<ResponseStatistic> { Filter = x => !string.IsNullOrWhiteSpace(x.ResponseStatus) && x.ResponseTime >= startTime && x.ResponseTime < endTime },
//             cancellationToken: cancellationToken);
//         if (dataList is { Count: > 0 })
//         {
//             var categoryList = new List<string>();
//             var createdList = new List<int>();
//             var readyList = new List<int>();
//             var skippedList = new List<int>();
//             var novideoList = new List<int>();
//
//             for (int day = 0; day < dateEnd.Day; day++)
//             {
//                 var loopDate = dateStart.AddDays(day);
//                 categoryList.Add(loopDate.Day.ToString().PadLeft(2, '0'));
//
//                 double startLoopTime = loopDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//                 double endLoopTime = loopDate.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//
//                 createdList.Add(
//                     dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.CREATED.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime).Sum(x => (int)x.ResponseCount));
//                 readyList.Add(dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.READY.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime).Sum(x => (int)x.ResponseCount));
//                 skippedList.Add(dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.SKIPPED_PATH.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime)
//                     .Sum(x => (int)x.ResponseCount));
//                 novideoList.Add(dataList.Where(x => x.ResponseStatus.Equals(AppContentPublicStatus.NO_ANALYSIS_VIDEO.ToLower(new CultureInfo("en-US"))) && x.ResponseTime >= startLoopTime && x.ResponseTime < endLoopTime)
//                     .Sum(x => (int)x.ResponseCount));
//             }
//
//             return new MonthlyResponsesAnalysisDto
//             {
//                 Categories = categoryList.ToArray(),
//                 Series =
//                 [
//                     new() { Name = L[AppContentPublicStatus.CREATED].ToString(), Data = createdList.ToArray() }, new() { Name = L[AppContentPublicStatus.READY].ToString(), Data = readyList.ToArray() },
//                     new() { Name = L[AppContentPublicStatus.SKIPPED_PATH].ToString(), Data = skippedList.ToArray() }, new() { Name = L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString(), Data = novideoList.ToArray() }
//                 ]
//             };
//         }
//
//         var categoryTempList = new List<string>();
//         var zeroDataList = new List<int>();
//         for (int day = 0; day < dateEnd.Day; day++)
//         {
//             var loopDate = dateStart.AddDays(day);
//             categoryTempList.Add(loopDate.Day.ToString().PadLeft(2, '0'));
//
//             zeroDataList.Add(0);
//         }
//
//         return new MonthlyResponsesAnalysisDto
//         {
//             Categories = categoryTempList.ToArray(),
//             Series =
//             [
//                 new() { Name = L[AppContentPublicStatus.CREATED].ToString(), Data = zeroDataList.ToArray() }, new() { Name = L[AppContentPublicStatus.READY].ToString(), Data = zeroDataList.ToArray() },
//                 new() { Name = L[AppContentPublicStatus.SKIPPED_PATH].ToString(), Data = zeroDataList.ToArray() }, new() { Name = L[AppContentPublicStatus.NO_ANALYSIS_VIDEO].ToString(), Data = zeroDataList.ToArray() }
//             ]
//         };
//     }
//
//     public async Task DashboardResponseStatisticQueryAsync(DashboardResponseStatisticQueryEto input, string correlationId = null)
//     {
//         if (input?.AppClientId == null)
//         {
//             throw new BaseHttpException((int)HttpStatusCode.BadRequest);
//         }
//
//         var client = await _clientRepository.GetSingleOrDefaultAsync(x => x.Id == input.AppClientId, selector: s => new { s.Id, s.TenantId, s.DomainName });
//         if (client == null)
//         {
//             throw new BaseHttpException((int)HttpStatusCode.NotFound);
//         }
//
//         _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "BEGIN");
//         var clientFirstVisitTime = await _appContentVisitRepository.GetFirstOrDefaultAsync(
//             predicate: x => x.ClientId == client.Id && x.VisitTimeLine > 2501010000,
//             orderByEntity: o => o.OrderBy(x => x.VisitTimeLine),
//             selector: s => new { s.VisitTimeLine },
//             cancellationToken: CancellationToken.None
//         );
//         if (clientFirstVisitTime?.VisitTimeLine > 0)
//         {
//             string value = clientFirstVisitTime.VisitTimeLine.ToString("D10");
//             if (DateTime.TryParseExact(
//                     value,
//                     "yyMMddHHmm",
//                     CultureInfo.InvariantCulture,
//                     DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var oldestDay
//                 ))
//             {
//                 var workDate = oldestDay.ToUniversalTime().Date;
//
//                 // continue oldest day calculation
//                 uint startWorkTime = uint.Parse(workDate.ToString("yyMMddHHmm"));
//                 uint endWorkTime = uint.Parse(workDate.AddDays(1).ToString("yyMMddHHmm")) - 1;
//
//                 // var workdayClientVisits = await _appContentVisitRepository.GetFilterListAsync(clientId: client.Id, visitStartTime: startWorkTime, visitEndTime: endWorkTime);
//                 var workdayClientVisits = await _appContentVisitRepository.GetListAsync(
//                     options: new ListQueryOptions<AppContentVisit> { Filter = x => x.ClientId == client.Id && x.VisitTimeLine >= startWorkTime && x.VisitTimeLine < endWorkTime + 1, OrderByDynamic = null },
//                     selector: s => new { s.Id, s.VisitResponse },
//                     cancellationToken: CancellationToken.None);
//
//                 if (workdayClientVisits is { Count: > 0 })
//                 {
//                     double responseStartTime = workDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
//                     double responseEndTime = workDate.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds - 1;
//
//                     // CLEAR WORK DAY DASHBOARD VALUES
//                     await _responseStatisticRepository.BulkDeleteAsync(x => x.ClientId == client.Id
//                                                                             && x.ResponseTime >= (ulong)responseStartTime
//                                                                             && x.ResponseTime < (ulong)responseEndTime + 1);
//
//                     // ADD WORK DAY DASHBOARD VALUES
//                     int createdCount = workdayClientVisits.Count(x => x.VisitResponse.Equals(AppContentPublicStatus.CREATED.ToLower(new CultureInfo("en-US"))));
//                     if (createdCount > 0)
//                     {
//                         await _responseStatisticRepository.CreateAsync(
//                             tenantId: client.TenantId,
//                             clientId: client.Id,
//                             responseStatus: AppContentPublicStatus.CREATED,
//                             responseTime: (ulong)responseStartTime,
//                             responseCount: (ulong)createdCount);
//                     }
//
//                     int readyCount = workdayClientVisits.Count(x => x.VisitResponse.Equals(AppContentPublicStatus.READY.ToLower(new CultureInfo("en-US"))));
//                     if (readyCount > 0)
//                     {
//                         await _responseStatisticRepository.CreateAsync(
//                             tenantId: client.TenantId,
//                             clientId: client.Id,
//                             responseStatus: AppContentPublicStatus.READY,
//                             responseTime: (ulong)responseStartTime,
//                             responseCount: (ulong)readyCount);
//                     }
//
//                     int skippedCount = workdayClientVisits.Count(x => x.VisitResponse.Equals(AppContentPublicStatus.SKIPPED_PATH.ToLower(new CultureInfo("en-US"))));
//                     if (skippedCount > 0)
//                     {
//                         await _responseStatisticRepository.CreateAsync(
//                             tenantId: client.TenantId,
//                             clientId: client.Id,
//                             responseStatus: AppContentPublicStatus.SKIPPED_PATH,
//                             responseTime: (ulong)responseStartTime,
//                             responseCount: (ulong)skippedCount);
//                     }
//
//                     int novideoCount = workdayClientVisits.Count(x => string.IsNullOrWhiteSpace(x.VisitResponse) || x.VisitResponse.Equals(AppContentPublicStatus.NO_ANALYSIS_VIDEO.ToLower(new CultureInfo("en-US"))));
//                     if (novideoCount > 0)
//                     {
//                         await _responseStatisticRepository.CreateAsync(
//                             tenantId: client.TenantId,
//                             clientId: client.Id,
//                             responseStatus: AppContentPublicStatus.NO_ANALYSIS_VIDEO,
//                             responseTime: (ulong)responseStartTime,
//                             responseCount: (ulong)novideoCount);
//                     }
//
//                     if (workDate < DateTime.UtcNow.Date)
//                     {
//                         // CLEAR OLD WORK DAY VISIT VALUES
//                         await _appContentVisitRepository.BulkDeleteAsync(x =>
//                             x.ClientId == client.Id &&
//                             x.VisitTimeLine >= startWorkTime
//                             && x.VisitTimeLine < endWorkTime + 1);
//                     }
//
//                     // CLEAR OLD ( 2 MOTHS) DASHBOARD VALUES
//                     var clearDateArea = workDate.AddDays(-31 * 2);
//                     double clearEndTime = clearDateArea.AddDays(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds - 1;
//                     await _responseStatisticRepository.BulkDeleteAsync(x =>
//                         x.ClientId == client.Id
//                         && x.ResponseTime < (ulong)clearEndTime + 1);
//                 }
//                 else
//                 {
//                     _logger.LogError("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "ERROR", "VISIT_NOT_FOUND");
//                 }
//             }
//             else
//             {
//                 _logger.LogError("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "ERROR", "INVALID_VISIT_TIME");
//             }
//         }
//         else
//         {
//             _logger.LogWarning("Client[{ClientDomain}] | {OperationStatus} => {QueryResult}", client.DomainName, "SKIPPED", "NO_VISIT");
//         }
//
//         _logger.LogInformation("Client[{ClientDomain}] | {OperationStatus}", client.DomainName, "END");
//     }
// }