// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Hhs.VideoGeneratorService.Application.Contracts.Providers.Dtos.VideoGenerate.Creatomoate;

public class Modifications
{
    [JsonProperty("width")]
    public short VideoWidth { get; set;}

    [JsonProperty("height")]
    public short VideoHeight { get; set;}

    [JsonProperty("Jenerik-Start.source")]
    public string JenerikStartSource { get; set;}

    [JsonProperty("Audio1.source")]
    public string Audio1Source { get; set;}

    [JsonProperty("Image1.source")]
    public string Image1Source { get; set;}

    [JsonProperty("Text1.text")]
    public string Text1Text { get; set;}

    [JsonProperty("Shape1.fill_color")]
    public string Shape1FillColor { get; set;}

    [JsonProperty("Number1.background_color")]
    public string Number1BackgroundColor { get; set;}

    [JsonProperty("Audio2.source")]
    public string Audio2Source { get; set;}

    [JsonProperty("Image2.source")]
    public string Image2Source { get; set;}

    [JsonProperty("Text2.text")]
    public string Text2Text { get; set;}

    [JsonProperty("Shape2.fill_color")]
    public string Shape2FillColor { get; set;}

    [JsonProperty("Number2.background_color")]
    public string Number2BackgroundColor { get; set;}

    [JsonProperty("Audio3.source")]
    public string Audio3Source { get; set;}

    [JsonProperty("Image3.source")]
    public string Image3Source { get; set;}

    [JsonProperty("Text3.text")]
    public string Text3Text { get; set;}

    [JsonProperty("Shape3.fill_color")]
    public string Shape3FillColor { get; set;}

    [JsonProperty("Number3.background_color")]
    public string Number3BackgroundColor { get; set;}

    [JsonProperty("Audio4.source")]
    public string Audio4Source { get; set;}

    [JsonProperty("Image4.source")]
    public string Image4Source { get; set;}

    [JsonProperty("Text4.text")]
    public string Text4Text { get; set;}

    [JsonProperty("Shape4.fill_color")]
    public string Shape4FillColor { get; set;}

    [JsonProperty("Number4.background_color")]
    public string Number4BackgroundColor { get; set;}

    [JsonProperty("Audio5.source")]
    public string Audio5Source { get; set;}

    [JsonProperty("Image5.source")]
    public string Image5Source { get; set;}

    [JsonProperty("Text5.text")]
    public string Text5Text { get; set;}

    [JsonProperty("Shape5.fill_color")]
    public string Shape5FillColor { get; set;}

    [JsonProperty("Number5.background_color")]
    public string Number5BackgroundColor { get; set;}

    [JsonProperty("Logo.source")]
    public string LogoSource { get; set;}

    [JsonProperty("Jenerik-End.source")]
    public string JenerikEndSource { get; set;}



}