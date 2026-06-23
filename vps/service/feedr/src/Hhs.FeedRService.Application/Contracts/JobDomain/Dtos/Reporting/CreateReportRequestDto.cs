// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JetBrains.Annotations;
using Google.Ads.AdManager.V1;

namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos.Reporting;

public class CreateReportRequestDto
{
    [NotNull]
    public string AdUnit { get; set; }

    [NotNull]
    public string DisplayName { get; set; }

    [NotNull]
    public string Parent { get; set; }

    public string AdUnitIdTopLevel { get; set; }

    public ReportDefinition.Types.DateRange.Types.RelativeDateRange DateRange { get; set; }
}