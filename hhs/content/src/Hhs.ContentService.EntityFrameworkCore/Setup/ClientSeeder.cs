using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Setup;

public static class ClientSeeder
{
    public static async Task SeedAsync(ContentServiceDbContext db, IAppConsoleLogger logger)
    {
        #region reseller tenants

        var gazeteTenantId = Guid.Parse("C35FD197-E1FE-455D-B118-38404A69931E");
        var gazeteClientId = Guid.Parse("0b16bed1-ffdb-4066-a33d-c6ee074b72e7");
        string gazeteDomainName = "test.gazete.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: gazeteTenantId,
            clientId: gazeteClientId,
            domainName: gazeteDomainName,
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), gazeteClientId, "haber", ClientFilterTypes.IncludeFilter),
            ]
        );

        var sporTenantId = Guid.Parse("B109FAF7-5AA2-451C-AC3A-51682D6C06E6");
        var sporClientId = Guid.Parse("cc16bed1-ffdb-4066-a33d-c6ee074b72aa");
        string sporDomainName = "test.spor.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: sporTenantId,
            clientId: sporClientId,
            domainName: sporDomainName,
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), sporClientId, "haber", ClientFilterTypes.IncludeFilter),
            ]
        );

        #endregion

        #region demo-techsummus

        var demoTechSummusTenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede");
        var demoTechSummusClientId = Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d");
        string demoTechSummusDomainName = "demo.techsummus.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: demoTechSummusTenantId,
            clientId: demoTechSummusClientId,
            domainName: demoTechSummusDomainName,
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "son-dakika", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "dunya", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "ekonomi", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "magazin", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "spor", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region dunya

        var dunyaTenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5");
        var dunyaClientId = Guid.Parse("07ca0b44-c052-4970-b128-b7b5feb1a03b");
        string dunyaDomainName = "www.dunya.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: dunyaTenantId,
            clientId: dunyaClientId,
            domainName: dunyaDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "dunya", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "gundem", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "sektorler", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "ekonomi", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "is-dunyasi", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "ihracat", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "kriptopara", ClientFilterTypes.IncludeFilter),
            ]
        );

        #endregion

        #region kisa-dalga

        var kisaDalgaTenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395");
        var kisaDalgaClientId = Guid.Parse("e8265c9c-e52c-4daa-8dbe-30874e5fc02c");
        string kisaDalgaDomainName = "www.kisadalga.net";
        await GetOrCreateClientAsync(db, logger,
            tenantId: kisaDalgaTenantId,
            clientId: kisaDalgaClientId,
            domainName: kisaDalgaDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/gundem", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/ekonomi", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/dunya", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/teknoloji", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/yasam", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/politika", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/saglik", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/otomobil", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/detay", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/magazin", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/kadin", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/kultur-sanat", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/spor", ClientFilterTypes.IncludeFilter),
            ]
        );

        #endregion

        #region tam-indir

        var tamIndirTenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17");
        var tamindirClientId = Guid.Parse("18bf822c-175c-4e2d-b9e0-1c3f0b2fe013");
        string tamindirDomainName = "www.tamindir.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: tamIndirTenantId,
            clientId: tamindirClientId,
            domainName: tamindirDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), tamindirClientId, "haber", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region techno-to-day

        var technoToDayTenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717");
        var technoToDayClientId = Guid.Parse("37fbec7b-9db2-4b68-a108-56cbfd6aa9ca");
        string technoToDayDomainName = "www.technotoday.com.tr";
        await GetOrCreateClientAsync(db, logger,
            tenantId: technoToDayTenantId,
            clientId: technoToDayClientId,
            domainName: technoToDayDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), technoToDayClientId, "kategori", ClientFilterTypes.ExcludeFilter)
            ]
        );

        #endregion

        #region sondakika

        var sondakikaTenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7");
        var sondakikaClientId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb");
        string sondakikaDomainName = "www.sondakika.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: sondakikaTenantId,
            clientId: sondakikaClientId,
            domainName: sondakikaDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "guncel", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "dunya", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "ekonomi", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "spor", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "magazin", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "politika", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "finans", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "teknoloji", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "kultur-sanat", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "kadin", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "moda", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "otomobil", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "yasam", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "saglik", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "turizm", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "egitim", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "3-sayfa", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region t24

        var t24TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
        var t24ClientId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901");
        string t24DomainName = "www.t24.com.tr";
        await GetOrCreateClientAsync(db, logger,
            tenantId: t24TenantId,
            clientId: t24ClientId,
            domainName: t24DomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), t24ClientId, "haber", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region cnbce

        var cnbceTenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a");
        var cnbceClientId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e");
        string cnbceDomainName = "www.cnbce.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: cnbceTenantId,
            clientId: cnbceClientId,
            domainName: cnbceDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "piyasalar", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "veriler", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "is-dunyasi", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "enerji", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "girisim", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "fuar", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "dijital-varliklar", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "kripto", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "savunma-sanayii", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "borsa", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "sigorta", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "teknoloji", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "haberler", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "gayrimenkul", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "otomotiv", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "gundem", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "sirket-haberleri", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "doviz", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "altin", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "emtia", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region box-office-turkiye

        var boxOfficeTurkiyeTenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c");
        var boxOfficeTurkiyeClientId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a");
        string boxOfficeTurkiyeDomainName = "www.boxofficeturkiye.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: boxOfficeTurkiyeTenantId,
            clientId: boxOfficeTurkiyeClientId,
            domainName: boxOfficeTurkiyeDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), boxOfficeTurkiyeClientId, "haber", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region diyetkolik

        var diyetkolikTenantId = Guid.Parse("87bfc020-422f-448c-bd67-210b3b66f727");
        var diyetkolikClientId = Guid.Parse("23a70bfe-af27-490b-b936-72d2e1e5b7db");
        string diyetkolikDomainName = "www.diyetkolik.com";
        await GetOrCreateClientAsync(db, logger,
            tenantId: diyetkolikTenantId,
            clientId: diyetkolikClientId,
            domainName: diyetkolikDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), diyetkolikClientId, "icerik", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), diyetkolikClientId, "icerik/kategori", ClientFilterTypes.ExcludeFilter)
            ]
        );

        #endregion

        #region inStyle

        var inStyleTenantId = Guid.Parse("092f6779-ab33-4754-8d41-43e58b92bcb1");
        var inStyleClientId = Guid.Parse("5153eac5-5dd4-41b7-93ad-268eac8a948a");
        string inStyleDomainName = "www.instyle.com.tr";
        await GetOrCreateClientAsync(db, logger,
            tenantId: inStyleTenantId,
            clientId: inStyleClientId,
            domainName: inStyleDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            clientPathFilters:
            [
                new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "pop-kultur", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "moda", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "guzellik-welness", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "astroloji", ClientFilterTypes.IncludeFilter),
                new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "kadin", ClientFilterTypes.IncludeFilter)
            ]
        );

        #endregion
    }

    private static async Task GetOrCreateClientAsync(ContentServiceDbContext db, IAppConsoleLogger logger, Guid tenantId, Guid clientId, string domainName,
        ushort dailyDirectVideoGenerationLimit = 0,
        ushort dailyDirectVideoGenerationStartedUtcHour = 0,
        ushort dailyTrendVideoGenerationLimit = 0,
        ushort dailyTrendVideoGenerationStartedUtcHour = 0,
        ushort dailyTrendVideoWaitStatisticHour = 0,
        ushort dailyTrendVideoMinVisitCount = 0,
        ushort dailyAnalysisVideoGenerationLimit = 0,
        ushort dailyAnalysisVideoGenerationStartedUtcHour = 0,
        List<ClientPathFilter> clientPathFilters = null
    )
    {
        var client = await db.Clients.SingleOrDefaultAsync(x => x.Id == clientId);

        if (client is not null)
            return;

        string normaizedDomainName = StringOperations.Minimize(StringOperations.ReplaceInvalidChars(domainName));
        clientPathFilters ??= [];

        client = new Client(
            id: clientId,
            tenantId: tenantId,
            domainName: normaizedDomainName
        )
        {
            DailyDirectVideoGenerationLimit = dailyDirectVideoGenerationLimit,
            DailyDirectVideoGenerationStartedUtcHour = dailyDirectVideoGenerationStartedUtcHour,
            DailyTrendVideoGenerationLimit = dailyTrendVideoGenerationLimit,
            DailyTrendVideoGenerationStartedUtcHour = dailyTrendVideoGenerationStartedUtcHour,
            DailyTrendVideoWaitStatisticHour = dailyTrendVideoWaitStatisticHour,
            DailyTrendVideoMinVisitCount = dailyTrendVideoMinVisitCount,
            DailyAnalysisVideoGenerationLimit = dailyAnalysisVideoGenerationLimit,
            DailyAnalysisVideoGenerationStartedUtcHour = dailyAnalysisVideoGenerationStartedUtcHour
        };

        db.Clients.Add(client);
        if (clientPathFilters is { Count: > 0 })
        {
            await db.ClientPathFilters.AddRangeAsync(clientPathFilters);
        }

        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | {DomainName} CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
            normaizedDomainName,
            clientId.ToString(),
            tenantId.ToString()
        );
    }
}