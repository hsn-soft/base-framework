using System.Drawing;
using Hhs.VideoGeneratorService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;

public class CustomerConfiguration : CreationAuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    public string ClientName { get; private set; }

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

    private CustomerConfiguration()
    {
        ClientName = string.Empty;
    }

    internal CustomerConfiguration(Guid id, Guid tenantId, Guid clientId, string clientName,
        bool isEnabledVideoGeneration, VideoGenerationProviderTypes videoGenerationProviderType, object videoGenerationProviderSettings,
        AudioProviderTypes audioProviderType, object audioProviderSettings,
        bool isCustomerZoneActive, string customerZoneName, string customerBucketKey, string customerBucketSecret,bool isEnabledExternalAudioGeneration=false) : this()
    {
        Id = id;
        SetTenantId(tenantId);
        SetClientId(clientId);
        ClientName = clientName;

        IsEnabledVideoGeneration = isEnabledVideoGeneration;
        IsEnabledExternalAudioGeneration = isEnabledExternalAudioGeneration;
        VideoGenerationProviderType = videoGenerationProviderType;
        VideoGenerationProviderSettings = videoGenerationProviderSettings;
        AudioProviderType = audioProviderType;
        AudioProviderSettings = audioProviderSettings;

        IsCustomerZoneActive = isCustomerZoneActive;
        CustomerZoneName = customerZoneName;
        CustomerBucketKey = customerBucketKey;
        CustomerBucketSecret = customerBucketSecret;
    }

    internal void SetId(Guid id)
    {
        if (Id == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(Id)} is invalid", nameof(id));
        }

        Id = id;
    }

    internal void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(TenantId)} is invalid", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
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
    public string LogoUrl {get; set;}
    public short VideoWidth { get; set; }
    public short VideoHeight { get; set; }
}

public class ClientCreatomateSettings
{
    public string JenericUrl { get; set; }
    public string DirectVideoTemplateId { get; set; }
    public string AnalysisVideoTemplateId { get; set; }
    public string BackgroundColor { get; set; }
    public string LogoUrl {get; set;}
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
    public string LogoUrl {get; set;}

    public Point LogoPosition{get; set;}
}

public class ClientElevenLabsSettings
{
    public string VoiceId { get; set; }
    public string LanguageCode { get; set; }
}


