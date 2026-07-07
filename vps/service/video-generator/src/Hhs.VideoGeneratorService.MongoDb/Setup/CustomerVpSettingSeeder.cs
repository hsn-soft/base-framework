using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Utils;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Logging.Abstracts;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.MongoDb.Setup;

public static class CustomerVpSettingSeeder
{
    public static async Task SeedAsync(VideoGeneratorServiceDbContext db, IAppConsoleLogger logger)
    {
        #region scenarious

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario01CustomerId),
            audioProviderKey: ProviderKeys.AudioQuick,
            videoProviderKey: ProviderKeys.VideoQueueExternal,
            logoUrl: "https://tst01.scenario.com/demo-techsummus-48x48.png",
            directVideoTemplateId: "xxxxxx",
            analysisVideoTemplateId: "xxxxxxx",
            jenericUrl: "https://tst01.scenario.com/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario02CustomerId),
            audioProviderKey: ProviderKeys.AudioHQ,
            videoProviderKey: ProviderKeys.VideoQueueExternal,
            logoUrl: "https://tst01.scenario.com/demo-techsummus-48x48.png",
            directVideoTemplateId: "xxxxxx",
            analysisVideoTemplateId: "xxxxxxx",
            jenericUrl: "https://tst01.scenario.com/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario03CustomerId),
            audioProviderKey: ProviderKeys.AudioQuick,
            videoProviderKey: ProviderKeys.VideoQueueExternal,
            logoUrl: "https://tst01.scenario.com/demo-techsummus-48x48.png",
            directVideoTemplateId: "xxxxxx",
            analysisVideoTemplateId: "xxxxxxx",
            jenericUrl: "https://tst01.scenario.com/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario04CustomerId),
            audioProviderKey: ProviderKeys.AudioHQ,
            videoProviderKey: ProviderKeys.VideoQueueExternal,
            logoUrl: "https://tst01.scenario.com/demo-techsummus-48x48.png",
            directVideoTemplateId: "xxxxxx",
            analysisVideoTemplateId: "xxxxxxx",
            jenericUrl: "https://tst01.scenario.com/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario05CustomerId),
            audioProviderKey: null,
            videoProviderKey: ProviderKeys.VideoQueueInternal,
            logoUrl: "https://tst01.scenario.com/demo-techsummus-48x48.png",
            directVideoTemplateId: "xxxxxx",
            analysisVideoTemplateId: "xxxxxxx",
            jenericUrl: "https://tst01.scenario.com/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario06CustomerId),
            audioProviderKey: null,
            videoProviderKey: ProviderKeys.VideoQueueInternal,
            logoUrl: "https://tst01.scenario.com/demo-techsummus-48x48.png",
            directVideoTemplateId: "xxxxxx",
            analysisVideoTemplateId: "xxxxxxx",
            jenericUrl: "https://tst01.scenario.com/demotechsummus-jenerik.mp4",
            backgroundColor: "#FFFFFF"
        );

        #endregion

        #region demo-techsummus

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechSummusCustomerId),
            audioProviderKey: ProviderKeys.AudioQuick,
            videoProviderKey: ProviderKeys.VideoQueueExternal,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
            audioProviderKey: ProviderKeys.AudioElevenLabs,
            videoProviderKey: ProviderKeys.VideoCreatomate,
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
        Guid customerId, string audioProviderKey, string videoProviderKey,
        string logoUrl, string directVideoTemplateId, string analysisVideoTemplateId, string jenericUrl, string backgroundColor)
    {
        string scopeKey = ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform);

        var filter = Builders<CustomerVpSetting>.Filter.Eq(x => x.ScopeKey, scopeKey);

        long customerVpSettingsCount = await db.CustomerVpSettings.CountDocumentsAsync(filter);
        if (customerVpSettingsCount > 0)
        {
            return;
        }

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

        string customerZoneName = customerId.ToString("N");

        var newEntity = new CustomerVpSetting(
            id: Guid.CreateVersion7(),
            customerId: customerId,
            audioProviderKey: audioProviderKey,
            videoProviderKey: videoProviderKey
        )
        {
            IsEnabledVideoGeneration = true,
            VideoGenerationProviderSettings = creatomateSetting,
            AudioProviderSettings = elevenLabsSetting,
            IsCustomerZoneActive = true,
            CustomerZoneName = customerZoneName,
            CustomerBucketKey = null,
            CustomerBucketSecret = null
        };

        await db.CustomerVpSettings.InsertOneAsync(newEntity);

        logger.LogDebug("{WorkerName} | CUSTOMER_VP_SETTING DATA ADDED: scopeKey [ {scopeKey} ]", nameof(MongoSeederService), scopeKey);
    }
}