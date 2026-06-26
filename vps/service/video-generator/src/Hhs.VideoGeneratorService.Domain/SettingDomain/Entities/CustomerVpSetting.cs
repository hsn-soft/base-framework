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

    [CanBeNull] public string AudioProviderKey { get; private set; }

    [NotNull] public string VideoProviderKey { get; private set; }

    public bool IsEnabledVideoGeneration { get; set; }

    public object VideoGenerationProviderSettings { get; set; }

    public object AudioProviderSettings { get; set; }
    public bool IsCustomerZoneActive { get; set; }
    [CanBeNull] public string CustomerZoneName { get; set; }
    [CanBeNull] public string CustomerBucketKey { get; set; }
    [CanBeNull] public string CustomerBucketSecret { get; set; }

    private CustomerVpSetting()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        VideoProviderKey = string.Empty;
    }

    internal CustomerVpSetting(
        Guid customerId,
        [NotNull] string videoProviderKey,
        [CanBeNull] string audioProviderKey = null)
        : this(id: Guid.CreateVersion7(), customerId: customerId, audioProviderKey: audioProviderKey, videoProviderKey: videoProviderKey)
    {
    }

    internal CustomerVpSetting(Guid id,
        Guid customerId,
        [NotNull] string videoProviderKey,
        [CanBeNull] string audioProviderKey = null) : this()
    {
        Id = id;

        SetScopeKey(customerId);
        SetAudioProviderKey(audioProviderKey);
        SetVideoProviderKey(videoProviderKey);
    }

    private void SetScopeKey(Guid customerId)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform),
            $"{nameof(CustomerVpSetting)}:{nameof(ScopeKey)}",
            CustomerVpSettingConsts.ScopeKeyMaxLength
        );

    internal void SetAudioProviderKey(string audioProviderKey)
    {
        if (!string.IsNullOrWhiteSpace(audioProviderKey))
        {
            string checkAudioProviderKey = LocalizedModelValidator.NotNullOrWhiteSpace(audioProviderKey, $"{nameof(CustomerVpSetting)}:{nameof(AudioProviderKey)}", CustomerVpSettingConsts.AudioProviderKeyMaxLength);
            AudioProviderKey = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkAudioProviderKey));
        }
        else
        {
            AudioProviderKey = null;
        }
    }

    internal void SetVideoProviderKey(string videoProviderKey)
    {
        string checkVideoProviderKey = LocalizedModelValidator.NotNullOrWhiteSpace(videoProviderKey, $"{nameof(CustomerVpSetting)}:{nameof(VideoProviderKey)}", CustomerVpSettingConsts.VideoProviderKeyMaxLength);
        VideoProviderKey = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkVideoProviderKey));
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