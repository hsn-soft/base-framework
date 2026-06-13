using System.Drawing;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;

public class CustomerVpSetting : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public bool IsEnabledVideoGeneration { get; set; }

    public bool IsEnabledExternalAudioGeneration { get; set; }

    public VideoGenerationProviderTypes VideoGenerationProviderType { get; set; }
    public object VideoGenerationProviderSettings { get; set; }

    public AudioProviderTypes AudioProviderType { get; set; }
    public object AudioProviderSettings { get; set; }
    public bool IsCustomerZoneActive { get; set; }
    public string CustomerZoneName { get; set; }
    public string CustomerBucketKey { get; set; }
    public string CustomerBucketSecret { get; set; }

    private CustomerVpSetting()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        DomainName = string.Empty;
    }

    internal CustomerVpSetting(Guid customerId, [NotNull] string domainName) : this(Guid.CreateVersion7(), customerId, domainName)
    {
    }

    internal CustomerVpSetting(Guid id, Guid customerId, [NotNull] string domainName) : this()
    {
        Id = id;

        SetScopeKey(customerId);
        SetDomainName(domainName);
    }

    private void SetScopeKey(Guid customerId)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform),
            $"{nameof(CustomerVpSetting)}:{nameof(ScopeKey)}",
            CustomerVpSettingConsts.ScopeKeyMaxLength
        );

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(CustomerVpSetting)}:{nameof(DomainName)}", CustomerVpSettingConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }
}

public class ClientColossyanAiSettings
{
    public string ApiBaseUrl { get; set; }
    public string ApiKey { get; set; }
    public string VoiceId { get; set; }
    public string AvatarId { get; set; }
    public string VideoTitle { get; set; }
    public string Visibility { get; set; }

    public VideoFormatTypes VideoFormat { get; set; }
    public short VideoWidth { get; set; }
    public short VideoHeight { get; set; }
}

public class ClientYepicAiSettings
{
    public string ApiBaseUrl { get; set; }
    public string ApiKey { get; set; }
    public string VoiceId { get; set; }
    public string AvatarId { get; set; }
    public string VideoTitle { get; set; }
    public string Visibility { get; set; }
    public VideoFormatTypes VideoFormat { get; set; }
    public short VideoWidth { get; set; }
    public short VideoHeight { get; set; }
}

public class ClientHeyGenSettings
{
    public string AvatarId { get; set; }
    public string DirectVideoTemplateId { get; set; }
    public string AnalysisVideoTemplateId { get; set; }
    public string VoiceId { get; set; }
    public string BackgroundImageUrl { get; set; }
    public string LogoUrl { get; set; }
    public short VideoWidth { get; set; }
    public short VideoHeight { get; set; }
}

public class ClientCreatomateSettings
{
    public string JenericUrl { get; set; }
    public string DirectVideoTemplateId { get; set; }
    public string AnalysisVideoTemplateId { get; set; }
    public string BackgroundColor { get; set; }
    public string LogoUrl { get; set; }
    public short VideoWidth { get; set; }
    public short VideoHeight { get; set; }
}

public class ClientDidAiSettings
{
    // konuşan dayı
    public string PresenterId { get; set; }

    public string DriverId { get; set; }
    // DriverId = "hOIr_2INMA",
    // PresenterId = "matt-PEvEohn_gk",

    // konuşan dayı sesi
    public string ProviderType { get; set; }
    public string ProviderVoiceId { get; set; }

    public string ProviderModelId { get; set; }
    // ProviderType = "elevenlabs",
    // ProviderVoiceId = "onwK4e9ZLuTAKqWW03F9",
    // ProviderModelId = "eleven_multilingual_v2",

    // arkaalan müşteri seçimi
    public string BackgroundSourceUrl { get; set; }

    // BackgroundSourceUrl = "https://4fe789ab-0652-4e7b-bd35-07019058081d.b-cdn.net/StudioBgRed.jpg"
    public string LogoUrl { get; set; }

    public Point LogoPosition { get; set; }
}

public class ClientElevenLabsSettings
{
    public string VoiceId { get; set; }
    public string LanguageCode { get; set; }
}