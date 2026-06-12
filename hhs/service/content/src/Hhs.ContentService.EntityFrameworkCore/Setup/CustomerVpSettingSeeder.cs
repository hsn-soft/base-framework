using Hhs.ContentService.Domain.SettingDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts;
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
            dailyDirectVideoGenerationLimit: 100,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 1,
            dailyAnalysisVideoGenerationStartedUtcHour: 0,
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
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters:
            [
                "haber"
            ],
            excludePathFilters: []
        );

        #endregion

        #region tam-indir

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TamIndirCustomerId),
            domainName: "tamindir.com",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters: ["haber"],
            excludePathFilters: []
        );

        #endregion

        #region sondakika

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.SonDakikaCustomerId),
            domainName: "sondakika.com",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
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
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters: [],
            excludePathFilters:
            [
                "kategori"
            ]
        );

        #endregion

        #region kisa-dalga

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.KisaDalgaCustomerId),
            domainName: "kisadalga.net",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
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
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
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
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters:
            [
                "haber"
            ],
            excludePathFilters: []
        );

        #endregion

        #region diyetkolik

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DiyetKolikCustomerId),
            domainName: "diyetkolik.com",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters:
            [
                "icerik"
            ],
            excludePathFilters:
            [
                "icerik/kategori"
            ]
        );

        #endregion

        #region inStyle

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.InStyleCustomerId),
            domainName: "instyle.com.tr",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
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
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters: [],
            excludePathFilters: []
        );

        #endregion

        #region bloomberght

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BloomberghtCustomerId),
            domainName: "bloomberght.com",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
            includePathFilters: [],
            excludePathFilters: []
        );

        #endregion

        #region cnbce

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.CnbceCustomerId),
            domainName: "www.cnbce.com",
            dailyDirectVideoGenerationLimit: 0,
            dailyDirectVideoGenerationStartedUtcHour: 0,
            dailyTrendVideoGenerationLimit: 0,
            dailyTrendVideoGenerationStartedUtcHour: 8,
            dailyTrendVideoWaitStatisticHour: 2,
            dailyTrendVideoMinVisitCount: 100,
            dailyAnalysisVideoGenerationLimit: 0,
            dailyAnalysisVideoGenerationStartedUtcHour: 6,
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
    }

    private static async Task GetOrCreateCustomerVpSettingAsync(ContentServiceDbContext db, IAppConsoleLogger logger,
        Guid customerId, string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null,
        ushort dailyDirectVideoGenerationLimit = 0,
        ushort dailyDirectVideoGenerationStartedUtcHour = 0,
        ushort dailyTrendVideoGenerationLimit = 0,
        ushort dailyTrendVideoGenerationStartedUtcHour = 0,
        ushort dailyTrendVideoWaitStatisticHour = 0,
        ushort dailyTrendVideoMinVisitCount = 0,
        ushort dailyAnalysisVideoGenerationLimit = 0,
        ushort dailyAnalysisVideoGenerationStartedUtcHour = 0
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
            DailyDirectVideoGenerationLimit = dailyDirectVideoGenerationLimit,
            DailyDirectVideoGenerationStartedUtcHour = dailyDirectVideoGenerationStartedUtcHour,
            DailyTrendVideoGenerationLimit = dailyTrendVideoGenerationLimit,
            DailyTrendVideoGenerationStartedUtcHour = dailyTrendVideoGenerationStartedUtcHour,
            DailyTrendVideoWaitStatisticHour = dailyTrendVideoWaitStatisticHour,
            DailyTrendVideoMinVisitCount = dailyTrendVideoMinVisitCount,
            DailyAnalysisVideoGenerationLimit = dailyAnalysisVideoGenerationLimit,
            DailyAnalysisVideoGenerationStartedUtcHour = dailyAnalysisVideoGenerationStartedUtcHour
        };

        db.CustomerVpSettings.Add(customerVpSetting);

        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER_VP_SETTING DATA ADDED: scopeKey [ {scopeKey} ]", nameof(EfCoreSeederService),
            normaizedDomainName,
            scopeKey
        );
    }
}