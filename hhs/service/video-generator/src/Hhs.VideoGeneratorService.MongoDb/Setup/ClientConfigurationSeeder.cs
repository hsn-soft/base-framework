using System.Drawing;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Logging.Abstracts;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.MongoDb.Setup;

public static class ClientConfigurationSeeder
{
    public static async Task SeedAsync(VideoGeneratorServiceDbContext db, IAppConsoleLogger logger)
    {
        #region reseller tenants

        var gazeteTenantId = Guid.Parse("C35FD197-E1FE-455D-B118-38404A69931E");
        var gazeteClientId = Guid.Parse("0b16bed1-ffdb-4066-a33d-c6ee074b72e7");
        string gazeteDomainName = "test.gazete.com";
        await GetOrCreateClientConfigurationAsync(db, logger, gazeteTenantId, gazeteClientId, gazeteDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/demo-techsummus-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "71de19fbe60e4c7ca643045ffe83bb0a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/localhost-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        var sporTenantId = Guid.Parse("B109FAF7-5AA2-451C-AC3A-51682D6C06E6");
        var sporClientId = Guid.Parse("cc16bed1-ffdb-4066-a33d-c6ee074b72aa");
        string sporDomainName = "test.spor.com";
        await GetOrCreateClientConfigurationAsync(db, logger, sporTenantId, sporClientId, sporDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/demo-techsummus-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "71de19fbe60e4c7ca643045ffe83bb0a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/localhost-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region demo-techsummus

        var demoTechSummusTenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede");
        var demoTechSummusClientId = Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d");
        string demoTechSummusDomainName = "demo.techsummus.com";
        await GetOrCreateClientConfigurationAsync(db, logger, demoTechSummusTenantId, demoTechSummusClientId, demoTechSummusDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/demo-techsummus-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "71de19fbe60e4c7ca643045ffe83bb0a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region dunya

        var dunyaTenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5");
        var dunyaClientId = Guid.Parse("07ca0b44-c052-4970-b128-b7b5feb1a03b");
        string dunyaDomainName = "www.dunya.com";
        await GetOrCreateClientConfigurationAsync(db, logger, dunyaTenantId, dunyaClientId, dunyaDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/dunya-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "b5e26cfe3d6948388fa63276f566eac1",
            jenericUrl: "https://assets-techsummus.b-cdn.net/dunya-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region kisa-dalga

        var kisaDalgaTenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395");
        var kisaDalgaClientId = Guid.Parse("e8265c9c-e52c-4daa-8dbe-30874e5fc02c");
        string kisaDalgaDomainName = "www.kisadalga.net";
        await GetOrCreateClientConfigurationAsync(db, logger, kisaDalgaTenantId, kisaDalgaClientId, kisaDalgaDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/KisaDalgaLogoDark.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "71de19fbe60e4c7ca643045ffe83bb0a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/kisadalga-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region tam-indir

        var tamIndirTenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17");
        var tamindirClientId = Guid.Parse("18bf822c-175c-4e2d-b9e0-1c3f0b2fe013");
        string tamindirDomainName = "www.tamindir.com";
        await GetOrCreateClientConfigurationAsync(db, logger, tamIndirTenantId, tamindirClientId, tamindirDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/tamindir-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "350c0486-accb-4583-abaa-b0d70fcdea1d", //"2c88c05dc5f6487e9d566fee4083fd8e",
            jenericUrl: "https://assets-techsummus.b-cdn.net/tamindir-jenerik.mp4",
            backgroundColor: "rgba(0,126,231,0.5)"
        );

        #endregion

        #region techno-to-day

        var technoToDayTenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717");
        var technoToDayClientId = Guid.Parse("37fbec7b-9db2-4b68-a108-56cbfd6aa9ca");
        string technoToDayDomainName = "www.technotoday.com.tr";
        await GetOrCreateClientConfigurationAsync(db, logger, technoToDayTenantId, technoToDayClientId, technoToDayDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/technotoday-footer-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "a2a2ca3a1d474fbc9489251f0aeb2e7a",
            jenericUrl: "https://assets-techsummus.b-cdn.net/technotoday-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region sondakika

        var sondakikaTenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7");
        var sondakikaClientId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb");
        string sondakikaDomainName = "www.sondakika.com";
        await GetOrCreateClientConfigurationAsync(db, logger, sondakikaTenantId, sondakikaClientId, sondakikaDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/sondakika-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "15c206d38b0f441aa9fdfddd4303eb5d",
            jenericUrl: "https://assets-techsummus.b-cdn.net/sondakika-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region t24

        var t24TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
        var t24ClientId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901");
        string t24DomainName = "www.t24.com.tr";
        await GetOrCreateClientConfigurationAsync(db, logger, t24TenantId, t24ClientId, t24DomainName,
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

        #region cnbce

        var cnbceTenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a");
        var cnbceClientId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e");
        string cnbceDomainName = "www.cnbce.com";
        await GetOrCreateClientConfigurationAsync(db, logger, cnbceTenantId, cnbceClientId, cnbceDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/cnbce-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "350c0486-accb-4583-abaa-b0d70fcdea1d", //creatomate Audio Template
            //analysisVideoTemplateId: "47707f4358644148b586ebefe1520f9d", //Heygen Text Template
            jenericUrl: "https://assets-techsummus.b-cdn.net/cnbce-jenerik.mp4",
            backgroundColor: "rgba(0,30,90,0.5)"
        );

        #endregion

        #region box-office-turkiye

        var boxOfficeTurkiyeTenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c");
        var boxOfficeTurkiyeClientId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a");
        string boxOfficeTurkiyeDomainName = "www.boxofficeturkiye.com";
        await GetOrCreateClientConfigurationAsync(db, logger, boxOfficeTurkiyeTenantId, boxOfficeTurkiyeClientId, boxOfficeTurkiyeDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/boxofficeturkiye-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "xxxxxxxxxxx", // TODO: Set real template id
            jenericUrl: "https://assets-techsummus.b-cdn.net/boxoffice-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region diyetkolik

        var diyetkolikTenantId = Guid.Parse("87bfc020-422f-448c-bd67-210b3b66f727");
        var diyetkolikClientId = Guid.Parse("23a70bfe-af27-490b-b936-72d2e1e5b7db");
        string diyetkolikDomainName = "www.diyetkolik.com";
        await GetOrCreateClientConfigurationAsync(db, logger, diyetkolikTenantId, diyetkolikClientId, diyetkolikDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/diyetkolik-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "3c0e045ec26c43b186c9bc6054d3f2f3",
            jenericUrl: "https://assets-techsummus.b-cdn.net/diyetkolik-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region inStyle

        var inStyleTenantId = Guid.Parse("092f6779-ab33-4754-8d41-43e58b92bcb1");
        var inStyleClientId = Guid.Parse("5153eac5-5dd4-41b7-93ad-268eac8a948a");
        string inStyleDomainName = "www.instyle.com.tr";
        await GetOrCreateClientConfigurationAsync(db, logger, inStyleTenantId, inStyleClientId, inStyleDomainName,
            logoUrl: "https://assets-techsummus.b-cdn.net/instyle-video-logo-48x48.png",
            directVideoTemplateId: "51009d1aea634ac7b1115a5d62a306b9",
            analysisVideoTemplateId: "3c0e045ec26c43b186c9bc6054d3f2f3",
            jenericUrl: "https://assets-techsummus.b-cdn.net/instyle-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR CONFIG", nameof(MongoSeederService));
    }

    private static async Task GetOrCreateClientConfigurationAsync(VideoGeneratorServiceDbContext db, IAppConsoleLogger logger, Guid tenantId, Guid clientId, string domainName,
        string logoUrl, string directVideoTemplateId, string analysisVideoTemplateId, string jenericUrl, string backgroundColor)
    {
        var filter = Builders<CustomerConfiguration>.Filter.Eq(doc => doc.ClientId, clientId);

        long customerConfigurationsCount = await db.CustomerConfigurations.CountDocumentsAsync(filter);
        if (customerConfigurationsCount > 0)
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

        string customerZoneName = clientId.ToString("N");
        if (domainName.Equals("demo.techsummus.com"))
            customerZoneName = "assets-techsummus";


        var newEntity = new CustomerConfiguration(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            clientId: clientId,
            clientName: domainName,
            isEnabledVideoGeneration: true,
            videoGenerationProviderType: VideoGenerationProviderTypes.CREATOMATE,
            videoGenerationProviderSettings: creatomateSetting,
            audioProviderType: AudioProviderTypes.ELEVEN_LABS,
            audioProviderSettings: elevenLabsSetting,
            isCustomerZoneActive: true,
            customerZoneName: customerZoneName,
            customerBucketKey: null,
            customerBucketSecret: null, isEnabledExternalAudioGeneration: isEnabledExternalAudioGeneration
        );

        await db.CustomerConfigurations.InsertOneAsync(newEntity);

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER CONFIGURATION ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(MongoSeederService),
            domainName,
            clientId.ToString(),
            tenantId.ToString()
        );
    }
}