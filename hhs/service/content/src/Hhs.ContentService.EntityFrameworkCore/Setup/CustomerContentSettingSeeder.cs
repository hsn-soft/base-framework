using Hhs.ContentService.Domain.CustomerDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Setup;

public static class CustomerContentSettingSeeder
{
    public static async Task SeedAsync(ContentServiceDbContext db, IAppConsoleLogger logger)
    {
        #region reseller tenants

        var gazeteTenantId = Guid.Parse("C35FD197-E1FE-455D-B118-38404A69931E");
        var gazeteCustomerContentSettingId = Guid.Parse("0b16bed1-ffdb-4066-a33d-c6ee074b72e7");
        string gazeteDomainName = "test.gazete.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: gazeteTenantId,
            customerContentSettingId: gazeteCustomerContentSettingId,
            domainName: gazeteDomainName,
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), gazeteCustomerContentSettingId, "haber", CustomerSettingFilterTypes.IncludeFilter),
            ]
        );

        var sporTenantId = Guid.Parse("B109FAF7-5AA2-451C-AC3A-51682D6C06E6");
        var sporCustomerContentSettingId = Guid.Parse("cc16bed1-ffdb-4066-a33d-c6ee074b72aa");
        string sporDomainName = "test.spor.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: sporTenantId,
            customerContentSettingId: sporCustomerContentSettingId,
            domainName: sporDomainName,
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sporCustomerContentSettingId, "haber", CustomerSettingFilterTypes.IncludeFilter),
            ]
        );

        #endregion

        #region demo-techsummus

        var demoTechSummusTenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede");
        var demoTechSummusCustomerContentSettingId = Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d");
        string demoTechSummusDomainName = "demo.techsummus.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: demoTechSummusTenantId,
            customerContentSettingId: demoTechSummusCustomerContentSettingId,
            domainName: demoTechSummusDomainName,
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), demoTechSummusCustomerContentSettingId, "son-dakika", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), demoTechSummusCustomerContentSettingId, "dunya", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), demoTechSummusCustomerContentSettingId, "ekonomi", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), demoTechSummusCustomerContentSettingId, "magazin", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), demoTechSummusCustomerContentSettingId, "spor", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region dunya

        var dunyaTenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5");
        var dunyaCustomerContentSettingId = Guid.Parse("07ca0b44-c052-4970-b128-b7b5feb1a03b");
        string dunyaDomainName = "www.dunya.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: dunyaTenantId,
            customerContentSettingId: dunyaCustomerContentSettingId,
            domainName: dunyaDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "dunya", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "gundem", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "sektorler", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "ekonomi", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "is-dunyasi", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "ihracat", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), dunyaCustomerContentSettingId, "kriptopara", CustomerSettingFilterTypes.IncludeFilter),
            ]
        );

        #endregion

        #region kisa-dalga

        var kisaDalgaTenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395");
        var kisaDalgaCustomerContentSettingId = Guid.Parse("e8265c9c-e52c-4daa-8dbe-30874e5fc02c");
        string kisaDalgaDomainName = "www.kisadalga.net";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: kisaDalgaTenantId,
            customerContentSettingId: kisaDalgaCustomerContentSettingId,
            domainName: kisaDalgaDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/gundem", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/ekonomi", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/dunya", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/teknoloji", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/yasam", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/politika", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/saglik", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/otomobil", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/detay", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/magazin", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/kadin", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/kultur-sanat", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), kisaDalgaCustomerContentSettingId, "haber/spor", CustomerSettingFilterTypes.IncludeFilter),
            ]
        );

        #endregion

        #region tam-indir

        var tamIndirTenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17");
        var tamindirCustomerContentSettingId = Guid.Parse("18bf822c-175c-4e2d-b9e0-1c3f0b2fe013");
        string tamindirDomainName = "www.tamindir.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: tamIndirTenantId,
            customerContentSettingId: tamindirCustomerContentSettingId,
            domainName: tamindirDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), tamindirCustomerContentSettingId, "haber", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region techno-to-day

        var technoToDayTenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717");
        var technoToDayCustomerContentSettingId = Guid.Parse("37fbec7b-9db2-4b68-a108-56cbfd6aa9ca");
        string technoToDayDomainName = "www.technotoday.com.tr";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: technoToDayTenantId,
            customerContentSettingId: technoToDayCustomerContentSettingId,
            domainName: technoToDayDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), technoToDayCustomerContentSettingId, "kategori", CustomerSettingFilterTypes.ExcludeFilter)
            ]
        );

        #endregion

        #region sondakika

        var sondakikaTenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7");
        var sondakikaCustomerContentSettingId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb");
        string sondakikaDomainName = "www.sondakika.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: sondakikaTenantId,
            customerContentSettingId: sondakikaCustomerContentSettingId,
            domainName: sondakikaDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "guncel", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "dunya", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "ekonomi", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "spor", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "magazin", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "politika", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "finans", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "teknoloji", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "kultur-sanat", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "kadin", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "moda", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "otomobil", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "yasam", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "saglik", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "turizm", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "egitim", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), sondakikaCustomerContentSettingId, "3-sayfa", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region t24

        var t24TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
        var t24CustomerContentSettingId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901");
        string t24DomainName = "www.t24.com.tr";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: t24TenantId,
            customerContentSettingId: t24CustomerContentSettingId,
            domainName: t24DomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), t24CustomerContentSettingId, "haber", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region cnbce

        var cnbceTenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a");
        var cnbceCustomerContentSettingId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e");
        string cnbceDomainName = "www.cnbce.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: cnbceTenantId,
            customerContentSettingId: cnbceCustomerContentSettingId,
            domainName: cnbceDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "piyasalar", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "veriler", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "is-dunyasi", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "enerji", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "girisim", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "fuar", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "dijital-varliklar", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "kripto", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "savunma-sanayii", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "borsa", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "sigorta", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "teknoloji", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "haberler", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "gayrimenkul", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "otomotiv", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "gundem", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "sirket-haberleri", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "doviz", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "altin", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), cnbceCustomerContentSettingId, "emtia", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region box-office-turkiye

        var boxOfficeTurkiyeTenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c");
        var boxOfficeTurkiyeCustomerContentSettingId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a");
        string boxOfficeTurkiyeDomainName = "www.boxofficeturkiye.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: boxOfficeTurkiyeTenantId,
            customerContentSettingId: boxOfficeTurkiyeCustomerContentSettingId,
            domainName: boxOfficeTurkiyeDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), boxOfficeTurkiyeCustomerContentSettingId, "haber", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion

        #region diyetkolik

        var diyetkolikTenantId = Guid.Parse("87bfc020-422f-448c-bd67-210b3b66f727");
        var diyetkolikCustomerContentSettingId = Guid.Parse("23a70bfe-af27-490b-b936-72d2e1e5b7db");
        string diyetkolikDomainName = "www.diyetkolik.com";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: diyetkolikTenantId,
            customerContentSettingId: diyetkolikCustomerContentSettingId,
            domainName: diyetkolikDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), diyetkolikCustomerContentSettingId, "icerik", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), diyetkolikCustomerContentSettingId, "icerik/kategori", CustomerSettingFilterTypes.ExcludeFilter)
            ]
        );

        #endregion

        #region inStyle

        var inStyleTenantId = Guid.Parse("092f6779-ab33-4754-8d41-43e58b92bcb1");
        var inStyleCustomerContentSettingId = Guid.Parse("5153eac5-5dd4-41b7-93ad-268eac8a948a");
        string inStyleDomainName = "www.instyle.com.tr";
        await GetOrCreateCustomerContentSettingAsync(db, logger,
            tenantId: inStyleTenantId,
            customerContentSettingId: inStyleCustomerContentSettingId,
            domainName: inStyleDomainName,
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            customerContentSettingPathFilters:
            [
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), inStyleCustomerContentSettingId, "pop-kultur", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), inStyleCustomerContentSettingId, "moda", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), inStyleCustomerContentSettingId, "guzellik-welness", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), inStyleCustomerContentSettingId, "astroloji", CustomerSettingFilterTypes.IncludeFilter),
                new CustomerContentSettingPathFilter(Guid.CreateVersion7(), inStyleCustomerContentSettingId, "kadin", CustomerSettingFilterTypes.IncludeFilter)
            ]
        );

        #endregion
    }

    private static async Task GetOrCreateCustomerContentSettingAsync(ContentServiceDbContext db, IAppConsoleLogger logger,
        Guid tenantId, Guid customerContentSettingId, string domainName,
        ushort dailyDirectVideoGenerationLimit = 0,
        ushort dailyDirectVideoGenerationStartedUtcHour = 0,
        ushort dailyTrendVideoGenerationLimit = 0,
        ushort dailyTrendVideoGenerationStartedUtcHour = 0,
        ushort dailyTrendVideoWaitStatisticHour = 0,
        ushort dailyTrendVideoMinVisitCount = 0,
        ushort dailyAnalysisVideoGenerationLimit = 0,
        ushort dailyAnalysisVideoGenerationStartedUtcHour = 0,
        List<CustomerContentSettingPathFilter> customerContentSettingPathFilters = null
    )
    {
        var customerContentSetting = await db.CustomerContentSettings.SingleOrDefaultAsync(x => x.Id == customerContentSettingId);

        if (customerContentSetting is not null)
            return;

        string normaizedDomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(domainName));
        customerContentSettingPathFilters ??= [];

        customerContentSetting = new CustomerContentSetting(
            id: customerContentSettingId,
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

        db.CustomerContentSettings.Add(customerContentSetting);
        if (customerContentSettingPathFilters is { Count: > 0 })
        {
            await db.CustomerContentSettingPathFilters.AddRangeAsync(customerContentSettingPathFilters);
        }

        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER_CONTENT_SETTING DATA ADDED: CustomerContentSettingId [ {CustomerContentSettingId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
            normaizedDomainName,
            customerContentSettingId.ToString(),
            tenantId.ToString()
        );
    }
}