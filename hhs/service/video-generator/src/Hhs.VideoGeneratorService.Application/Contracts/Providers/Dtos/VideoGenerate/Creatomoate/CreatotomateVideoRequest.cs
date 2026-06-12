// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Creatomoate;

public class CreatotomateVideoRequest
{
    [JsonProperty("template_id")]
    public string TemplateId { get; set;}

    [JsonProperty("modifications")]
    public Modifications Modifications { get; set;}
}

public sealed class CreatomateVideoResponse
{
    [JsonProperty("id")]
    public string VideoId { get; set; }
}

public sealed class CreatomateVideoQueryResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("url")]
    public string VideoUrl { get; set; }
}
