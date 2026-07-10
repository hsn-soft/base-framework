using Hhs.ContentService.Domain.SettingDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Constants;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Setup;

public static class CustomerVpSettingSeeder
{
    public static async Task SeedAsync(ContentServiceDbContext db, IAppConsoleLogger logger)
    {
        #region demo-techsummus

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechSummusCustomerId),
            domainName: "demo.techsummus.com",
            includePathFilters:
            [
                "son-dakika",
                "dunya",
                "ekonomi",
                "magazin",
                "spor",
            ],
            excludePathFilters: []
        );

        #endregion

        #region t24

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.T24CustomerId),
            domainName: "t24.com.tr",
            includePathFilters: ["haber"],
            excludePathFilters: []
        );

        #endregion

        #region tam-indir

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TamIndirCustomerId),
            domainName: "tamindir.com",
            includePathFilters: ["haber"],
            excludePathFilters: []
        );

        #endregion

        #region sondakika

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.SonDakikaCustomerId),
            domainName: "sondakika.com",
            includePathFilters:
            [
                "guncel",
                "dunya",
                "ekonomi",
                "spor",
                "magazin",
                "politika",
                "finans",
                "teknoloji",
                "kultur-sanat",
                "kadin",
                "moda",
                "otomobil",
                "yasam",
                "saglik",
                "turizm",
                "egitim",
                "3-sayfa",
            ],
            excludePathFilters: []
        );

        #endregion

        #region techno-to-day

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechnoTodayCustomerId),
            domainName: "technotoday.com.tr",
            includePathFilters: [],
            excludePathFilters: ["kategori"]
        );

        #endregion

        #region kisa-dalga

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.KisaDalgaCustomerId),
            domainName: "kisadalga.net",
            includePathFilters:
            [
                "haber/gundem",
                "haber/ekonomi",
                "haber/dunya",
                "haber/teknoloji",
                "haber/yasam",
                "haber/politika",
                "haber/saglik",
                "haber/otomobil",
                "haber/detay",
                "haber/magazin",
                "haber/kadin",
                "haber/kultur-sanat",
                "haber/spor",
            ],
            excludePathFilters: []
        );

        #endregion

        #region dunya

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DunyaCustomerId),
            domainName: "dunya.com",
            includePathFilters:
            [
                "dunya",
                "gundem",
                "sektorler",
                "ekonomi",
                "is-dunyasi",
                "ihracat",
                "kriptopara",
            ],
            excludePathFilters: []
        );

        #endregion

        #region box-office-turkiye

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BoxofficeCustomerId),
            domainName: "boxofficeturkiye.com",
            includePathFilters: ["haber"],
            excludePathFilters: []
        );

        #endregion

        #region diyetkolik

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DiyetKolikCustomerId),
            domainName: "diyetkolik.com",
            includePathFilters: ["icerik"],
            excludePathFilters: ["icerik/kategori"]
        );

        #endregion

        #region inStyle

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.InStyleCustomerId),
            domainName: "instyle.com.tr",
            includePathFilters:
            [
                "pop-kultur",
                "moda",
                "guzellik-welness",
                "astroloji",
                "kadin",
            ],
            excludePathFilters: []
        );

        #endregion

        #region haberturk

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.HaberturkCustomerId),
            domainName: "haberturk.com",
            includePathFilters: [],
            excludePathFilters: []
        );

        #endregion

        #region bloomberght

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BloomberghtCustomerId),
            domainName: "bloomberght.com",
            includePathFilters: [],
            excludePathFilters: []
        );

        #endregion

        #region cnbce

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.CnbceCustomerId),
            domainName: "www.cnbce.com",
            includePathFilters:
            [
                "piyasalar",
                "veriler",
                "is-dunyasi",
                "enerji",
                "girisim",
                "fuar",
                "dijital-varliklar",
                "kripto",
                "savunma-sanayii",
                "borsa",
                "sigorta",
                "teknoloji",
                "haberler",
                "gayrimenkul",
                "otomotiv",
                "gundem",
                "sirket-haberleri",
                "doviz",
                "altin",
                "emtia",
            ],
            excludePathFilters: []
        );

        #endregion

        #region scenarious

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario01CustomerId),
            domainName: "tst01.scenario.com",
            includePathFilters: ["haber"]
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario02CustomerId),
            domainName: "tst02.scenario.com",
            includePathFilters: ["haber"]
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario03CustomerId),
            domainName: "tst03.scenario.com",
            includePathFilters: ["haber"]
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario04CustomerId),
            domainName: "tst04.scenario.com",
            includePathFilters: ["haber"]
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario05CustomerId),
            domainName: "tst05.scenario.com",
            includePathFilters: ["haber"]
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario06CustomerId),
            domainName: "tst06.scenario.com",
            includePathFilters: ["haber"]
        );

        #endregion
    }

    private static async Task GetOrCreateCustomerVpSettingAsync(ContentServiceDbContext db, IAppConsoleLogger logger,
        Guid customerId, string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null,
        // direct-video
        ushort dailyDirectVideoGenerationLimit = 0,
        ushort dailyDirectVideoGenerationStartedUtcHour = 0,
        // trend-video
        ushort dailyTrendVideoGenerationLimit = 0,
        ushort dailyTrendVideoGenerationStartedUtcHour = 8,
        ushort dailyTrendContentWaitStatisticHour = 2,
        ushort dailyTrendContentMinVisitCount = 100,
        // analysis-video
        ushort dailyAnalysisVideoGenerationLimit = 1,
        ushort dailyAnalysisVideoGenerationStartedUtcHour = 6,
        ushort dailyAnalysisContentWaitStatisticHour = 2,
        ushort dailyAnalysisContentMinVisitCount = 100,
        ushort dailyAnalysisVideoItemLimit = 5
    )
    {
        string scopeKey = ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform);
        string normaizedDomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(domainName));

        var customerVpSetting = await db.CustomerVpSettings.SingleOrDefaultAsync(x => x.ScopeKey == scopeKey || x.DomainName == normaizedDomainName);

        if (customerVpSetting is not null)
            return;

        includePathFilters ??= [];
        excludePathFilters ??= [];

        customerVpSetting = new CustomerVpSetting(
            customerId: customerId,
            domainName: normaizedDomainName,
            includePathFilters: includePathFilters,
            excludePathFilters: excludePathFilters
        )
        {
            IsBlocked = false,
            // Direct Video
            DailyDirectVideoGenerationLimit = dailyDirectVideoGenerationLimit,
            DailyDirectVideoGenerationStartedUtcHour = dailyDirectVideoGenerationStartedUtcHour,
            // Trend Video
            DailyTrendVideoGenerationLimit = dailyTrendVideoGenerationLimit,
            DailyTrendVideoGenerationStartedUtcHour = dailyTrendVideoGenerationStartedUtcHour,
            DailyTrendContentWaitStatisticHour = dailyTrendContentWaitStatisticHour,
            DailyTrendContentMinVisitCount = dailyTrendContentMinVisitCount,
            // Analysis Video
            DailyAnalysisVideoGenerationLimit = dailyAnalysisVideoGenerationLimit,
            DailyAnalysisVideoGenerationStartedUtcHour = dailyAnalysisVideoGenerationStartedUtcHour,
            DailyAnalysisContentWaitStatisticHour = dailyAnalysisContentWaitStatisticHour,
            DailyAnalysisContentMinVisitCount = dailyAnalysisContentMinVisitCount,
            DailyAnalysisVideoItemLimit = dailyAnalysisVideoItemLimit
        };

        db.CustomerVpSettings.Add(customerVpSetting);

        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER_VP_SETTING DATA ADDED: scopeKey [ {scopeKey} ]", nameof(EfCoreSeederService),
            normaizedDomainName,
            scopeKey
        );
    }
}