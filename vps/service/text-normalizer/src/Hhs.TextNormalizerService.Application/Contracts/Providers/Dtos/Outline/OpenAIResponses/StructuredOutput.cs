using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline.OpenAIResponses;

public class StructuredOutput
{
    [EnumDataType(typeof(Categories))]
    [Description("Content category")]
    public List<Categories> Category { get; set; }

    //[UsedImplicitly]
    [Description("Content tags")]
    public List<string> Tags { get; set; }

    //[UsedImplicitly]
    [Description("Spot")]
    public string Spot { get; set; }

    //[UsedImplicitly]
    [Description("Title")]
    public string Title { get; set; }

    //[UsedImplicitly]
    [Description("Summary")]
    public string Summary { get; set; }
}

public enum Categories
{
    [UsedImplicitly]
    Politika,
    [UsedImplicitly]
    Spor,
    [UsedImplicitly]
    Sanat,
    [UsedImplicitly]
    Egitim,
    [UsedImplicitly]
    Finans,
    [UsedImplicitly]
    Savas,
    [UsedImplicitly]
    Dunya,
    [UsedImplicitly]
    Yerel,
    [UsedImplicitly]
    Teknoloji,
    [UsedImplicitly]
    Magazin,
    [UsedImplicitly]
    HavaDurumu,
    [UsedImplicitly]
    Mizah,
    [UsedImplicitly]
    Eglence,
    [UsedImplicitly]
    Yasam,
    [UsedImplicitly]
    SonDakika,
    [UsedImplicitly]
    Saglik,
    [UsedImplicitly]
    Bilim,
    [UsedImplicitly]
    Seyahat,
    [UsedImplicitly]
    Kultur
}
