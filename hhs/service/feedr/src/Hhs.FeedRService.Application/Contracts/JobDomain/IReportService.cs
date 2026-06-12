// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Ads.AdManager.V1;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;

namespace Hhs.FeedRService.Application.Contracts.JobDomain;

public interface IGoogleReportService
{
    Task<string> CreateReportAsync(CreateReportRequest request);
    CreateReportRequest CreateReportRequest(CreateReportRequestDto input);
    Task<GenerateSummaryReportRes> GenerateSummaryReportAsync(Guid appClientId, string dateRange);
    Task<GenerateSummaryReportRes> GenerateSummaryReportForAllAsync(string dateRange);
    Task<string> RetrieveInventoriesAsync(Guid appClientId);
    
}