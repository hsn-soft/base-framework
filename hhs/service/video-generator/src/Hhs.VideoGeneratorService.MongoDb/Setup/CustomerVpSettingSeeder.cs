using System.Drawing;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.MongoDb.Setup;

public static class CustomerVpSettingSeeder
{
    public static async Task SeedAsync(VideoGeneratorServiceDbContext db, IAppConsoleLogger logger)
    {
        #region demo-techsummus

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechSummusCustomerId),
            domainName: "demo.techsummus.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/demo-techsummus-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "71de19fbe60e4c7ca643045ffe83bb0a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region t24

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.T24CustomerId),
            domainName: "t24.com.tr",
            logoUrl: "https://assets-techsummus.b-cdn.net/t24-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "350c0486-accb-4583-abaa-b0d70fcdea1d", //creatomate Audio Template
            //analysisVideoTemplateId:"5762b7004ad143bf844573038546a69c", //Heygen Text Template
            jenericUrl: "https://assets-techsummus.b-cdn.net/t24-jenerik.mp4",
            backgroundColor: "rgba(2,127,198,0.5)"
            //directVideoTemplateId: "0c4835fffb0148bcaa6611613443a9ca", //Heygen AudioTemplate
            //analysisVideoTemplateId: "0b8af3f116424a7c8b22521d049b228a"  //Heygen AudioTemplate
        );

        #endregion

        #region tam-indir

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TamIndirCustomerId),
            domainName: "tamindir.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/tamindir-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "350c0486-accb-4583-abaa-b0d70fcdea1d", //"2c88c05dc5f6487e9d566fee4083fd8e",
            jenericUrl: "https://assets-techsummus.b-cdn.net/tamindir-jenerik.mp4",
            backgroundColor: "rgba(0,126,231,0.5)"
        );

        #endregion

        #region sondakika

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.SonDakikaCustomerId),
            domainName: "sondakika.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/sondakika-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "15c206d38b0f441aa9fdfddd4303eb5d",
            jenericUrl: "https://assets-techsummus.b-cdn.net/sondakika-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region techno-to-day

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechnoTodayCustomerId),
            domainName: "technotoday.com.tr",
            logoUrl: "https://assets-techsummus.b-cdn.net/technotoday-footer-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "a2a2ca3a1d474fbc9489251f0aeb2e7a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/technotoday-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region kisa-dalga

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.KisaDalgaCustomerId),
            domainName: "kisadalga.net",
            logoUrl: "https://assets-techsummus.b-cdn.net/KisaDalgaLogoDark.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "71de19fbe60e4c7ca643045ffe83bb0a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/kisadalga-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region dunya

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DunyaCustomerId),
            domainName: "dunya.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/dunya-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "b5e26cfe3d6948388fa63276f566eac1",
            jenericUrl: "https://assets-techsummus.b-cdn.net/dunya-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region box-office-turkiye

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BoxofficeCustomerId),
            domainName: "boxofficeturkiye.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/boxofficeturkiye-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "xxxxxxxxxxx", // TODO: Set real template id
            jenericUrl: "https://assets-techsummus.b-cdn.net/boxoffice-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region diyetkolik

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DiyetKolikCustomerId),
            domainName: "diyetkolik.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/diyetkolik-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "3c0e045ec26c43b186c9bc6054d3f2f3",
            jenericUrl: "https://assets-techsummus.b-cdn.net/diyetkolik-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region inStyle

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.InStyleCustomerId),
            domainName: "instyle.com.tr",
            logoUrl: "https://assets-techsummus.b-cdn.net/instyle-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "3c0e045ec26c43b186c9bc6054d3f2f3",
            jenericUrl: "https://assets-techsummus.b-cdn.net/instyle-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region haberturk

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.HaberturkCustomerId),
            domainName: "haberturk.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/instyle-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "3c0e045ec26c43b186c9bc6054d3f2f3",
            jenericUrl: "https://assets-techsummus.b-cdn.net/instyle-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region bloomberght

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BloomberghtCustomerId),
            domainName: "bloomberght.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/instyle-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "3c0e045ec26c43b186c9bc6054d3f2f3",
            jenericUrl: "https://assets-techsummus.b-cdn.net/instyle-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region cnbce

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.CnbceCustomerId),
            domainName: "cnbce.com",
            logoUrl: "https://assets-techsummus.b-cdn.net/cnbce-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "350c0486-accb-4583-abaa-b0d70fcdea1d", //creatomate Audio Template
            //analysisVideoTemplateId: "47707f4358644148b586ebefe1520f9d", //Heygen Text Template
            jenericUrl: "https://assets-techsummus.b-cdn.net/cnbce-jenerik.mp4",
            backgroundColor: "rgba(0,30,90,0.5)"
        );

        #endregion

        logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR CONFIG", nameof(MongoSeederService));
    }

    private static async Task GetOrCreateCustomerVpSettingAsync(VideoGeneratorServiceDbContext db, IAppConsoleLogger logger,
        Guid customerId, string domainName,
        string logoUrl, string directVideoTemplateId, string analysisVideoTemplateId, string jenericUrl, string backgroundColor)
    {
        string scopeKey = ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform);
        string normaizedDomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(domainName));

        var filter = Builders<CustomerVpSetting>.Filter.Or(
            Builders<CustomerVpSetting>.Filter.Eq(x => x.ScopeKey, scopeKey),
            Builders<CustomerVpSetting>.Filter.Eq(x => x.DomainName, normaizedDomainName)
        );

        long customerVpSettingsCount = await db.CustomerVpSettings.CountDocumentsAsync(filter);
        if (customerVpSettingsCount > 0)
        {
            return;
        }

        var colossyanSetting = new ClientColossyanAiSettings
        {
            ApiBaseUrl = "https://api.yepic.ai",
            ApiKey = "213957bc-41c8-4f5f-865c-2c341e37166a",
            VideoTitle = "Generic Talking Photo",
            Visibility = "Public",
            VideoFormat = VideoFormatTypes.mp4,
            VideoWidth = 1080,
            VideoHeight = 720,

            // KONUŞAN DAYI
            AvatarId = "ea03926b-a6fb-4bce-88d4-8c6bd35aaf05",
            // KONUSAN DAYI SESI
            VoiceId = "tr-TR-AhmetNeural"
        };
        var yepicSetting = new ClientYepicAiSettings
        {
            ApiBaseUrl = "https://api.yepic.ai",
            ApiKey = "213957bc-41c8-4f5f-865c-2c341e37166a",
            VideoTitle = "Generic Talking Photo",
            Visibility = "Public",
            VideoFormat = VideoFormatTypes.mp4,
            VideoWidth = 1080,
            VideoHeight = 720,

            // KONUŞAN DAYI
            AvatarId = "ea03926b-a6fb-4bce-88d4-8c6bd35aaf05",
            // KONUSAN DAYI SESI
            VoiceId = "tr-TR-AhmetNeural"
        };
        var didSetting = new ClientDidAiSettings
        {
            // konuşan dayı
            DriverId = "hOIr_2INMA",
            PresenterId = "matt-PEvEohn_gk",

            // konuşan dayı sesi
            ProviderType = "elevenlabs",
            ProviderVoiceId = "onwK4e9ZLuTAKqWW03F9",
            ProviderModelId = "eleven_multilingual_v2",

            // arkaalan müşteri seçimi
            BackgroundSourceUrl = "https://assets-techsummus.b-cdn.net/NewsStudioBlueBg.jpg",
            LogoUrl = logoUrl,
            LogoPosition = new Point(x: 30, y: 640)
        };
        var heyGenSetting = new ClientHeyGenSettings
        {
            // Direct Video Setting
            DirectVideoTemplateId = directVideoTemplateId,
            AvatarId = "Brent_sitting_office_front",
            VoiceId = "ff2ecc8fbdef4273a28bed7b5e35bb57",
            BackgroundImageUrl = "https://assets-techsummus.b-cdn.net/NewsStudioBlueBg.jpg",

            //Analysis Video Setting
            AnalysisVideoTemplateId = analysisVideoTemplateId,

            //General Setting
            LogoUrl = logoUrl,
            VideoWidth = 640,
            VideoHeight = 360
        };
        var creatomateSetting = new ClientCreatomateSettings
        {
            JenericUrl = jenericUrl,
            DirectVideoTemplateId = directVideoTemplateId,
            AnalysisVideoTemplateId = analysisVideoTemplateId,
            BackgroundColor = backgroundColor,
            LogoUrl = logoUrl,
            VideoWidth = 640,
            VideoHeight = 360
        };
        var elevenLabsSetting = new ClientElevenLabsSettings { VoiceId = "onwK4e9ZLuTAKqWW03F9", LanguageCode = "tr" };

        //bool isEnabledExternalAudioGeneration = domainName.Equals("www.tamindir.com");
        bool isEnabledExternalAudioGeneration = true;

        string customerZoneName = customerId.ToString("N");
        if (domainName.Equals("demo.techsummus.com"))
            customerZoneName = "assets-techsummus";

        var newEntity = new CustomerVpSetting(
            id: Guid.CreateVersion7(),
            customerId: customerId,
            domainName: domainName
        )
        {
            IsEnabledVideoGeneration = true,
            VideoGenerationProviderType = VideoGenerationProviderTypes.CREATOMATE,
            VideoGenerationProviderSettings = creatomateSetting,
            AudioProviderType = AudioProviderTypes.ELEVEN_LABS,
            AudioProviderSettings = elevenLabsSetting,
            IsCustomerZoneActive = true,
            CustomerZoneName = customerZoneName,
            CustomerBucketKey = null,
            CustomerBucketSecret = null,
            IsEnabledExternalAudioGeneration = isEnabledExternalAudioGeneration
        };

        await db.CustomerVpSettings.InsertOneAsync(newEntity);

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER_VP_SETTING DATA ADDED: scopeKey [ {scopeKey} ]", nameof(MongoSeederService),
            normaizedDomainName,
            scopeKey
        );
    }
}