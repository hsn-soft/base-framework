using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.EntityFrameworkCore;

public sealed class EfCoreSeederService : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EfCoreSeederService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogInformation("{WorkerName} | {OperationStatus}", nameof(EfCoreSeederService), "START");

        bool isReadyDatabase = false;
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentServiceDbContext>();
        try
        {
            if (dbContext.Database.CanConnectAsync(cancellationToken).GetAwaiter().GetResult())
            {
                if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken)).Any())
                {
                    // apply pending migrations
                    await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                    logger.LogInformation("{WorkerName} | PENDING MIGRATIONS SUCCESSFULLY APPLIED", nameof(EfCoreSeederService));
                }
                else
                {
                    logger.LogInformation("{WorkerName} | EVERYTHING IS UP TO DATE", nameof(EfCoreSeederService));
                }
            }
            else
            {
                // first creation
                await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogInformation("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(EfCoreSeederService));
            }

            isReadyDatabase = true;
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(EfCoreSeederService), "FAIL", e.Message);
        }

        if (isReadyDatabase)
        {
            if (!dbContext.Clients.Any(x => !x.IsDeleted))
            {
                // Seed default clients
                var defaultTenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede");

                #region local-test

                var localhostClientId = Guid.Parse("0b16bed1-ffdb-4066-a33d-c6ee074b72e7");
                string localhostDomainName = "localhost";
                dbContext.Clients.Add(new Client(localhostClientId, defaultTenantId, localhostDomainName));
                logger.LogDebug("{WorkerName} | localhost CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    localhostClientId.ToString(),
                    defaultTenantId.ToString()
                );

                #endregion

                #region demo-techsummus

                var demoTechSummusClientId = Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d");
                string demoTechSummusDomainName = "demo.techsummus.com";
                var demoTechSummusEntity = new Client(demoTechSummusClientId, defaultTenantId, demoTechSummusDomainName);

                demoTechSummusEntity.DailyDirectVideoGenerationLimit = 100;
                demoTechSummusEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                demoTechSummusEntity.DailyTrendVideoGenerationLimit = 0;
                demoTechSummusEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                demoTechSummusEntity.DailyTrendVideoWaitStatisticHour = 2;
                demoTechSummusEntity.DailyTrendVideoMinVisitCount = 100;

                demoTechSummusEntity.DailyAnalysisVideoGenerationLimit = 1;
                demoTechSummusEntity.DailyAnalysisVideoGenerationStartedUtcHour = 0;

                dbContext.Clients.Add(demoTechSummusEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "son-dakika", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "dunya", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "ekonomi", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "magazin", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), demoTechSummusClientId, "spor", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | demo.techsummus.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    demoTechSummusClientId.ToString(),
                    defaultTenantId.ToString()
                );

                #endregion

                #region kisa-dalga

                var kisaDalgaTenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395");
                var kisaDalgaClientId = Guid.Parse("e8265c9c-e52c-4daa-8dbe-30874e5fc02c");
                string kisaDalgaDomainName = "www.kisadalga.net";
                var kisaDalgaEntity = new Client(kisaDalgaClientId, kisaDalgaTenantId, kisaDalgaDomainName);

                dbContext.Clients.Add(kisaDalgaEntity);
                // include filters
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/gundem", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/ekonomi", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/dunya", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/teknoloji", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/yasam", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/politika", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/saglik", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/otomobil", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/detay", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/magazin", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/kadin", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/kultur-sanat", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), kisaDalgaClientId, "haber/spor", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.kisadalga.net CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    kisaDalgaClientId.ToString(),
                    kisaDalgaTenantId.ToString()
                );

                #endregion

                #region dunya

                var dunyaTenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5");
                var dunyaClientId = Guid.Parse("07ca0b44-c052-4970-b128-b7b5feb1a03b");
                string dunyaDomainName = "www.dunya.com";
                var dunyaEntity = new Client(dunyaClientId, dunyaTenantId, dunyaDomainName);

                dunyaEntity.DailyDirectVideoGenerationLimit = 0;
                dunyaEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                dunyaEntity.DailyTrendVideoGenerationLimit = 0;
                dunyaEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                dunyaEntity.DailyTrendVideoWaitStatisticHour = 2;
                dunyaEntity.DailyTrendVideoMinVisitCount = 100;

                dunyaEntity.DailyAnalysisVideoGenerationLimit = 0;
                dunyaEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(dunyaEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "dunya", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "gundem", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "sektorler", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "ekonomi", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "is-dunyasi", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "ihracat", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), dunyaClientId, "kriptopara", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.dunya.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    dunyaClientId.ToString(),
                    dunyaTenantId.ToString()
                );

                #endregion

                #region tam-indir

                var tamIndirTenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17");
                var tamindirClientId = Guid.Parse("18bf822c-175c-4e2d-b9e0-1c3f0b2fe013");
                string tamindirDomainName = "www.tamindir.com";
                var tamindirEntity = new Client(tamindirClientId, tamIndirTenantId, tamindirDomainName);

                tamindirEntity.DailyDirectVideoGenerationLimit = 0;
                tamindirEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                tamindirEntity.DailyTrendVideoGenerationLimit = 0;
                tamindirEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                tamindirEntity.DailyTrendVideoWaitStatisticHour = 2;
                tamindirEntity.DailyTrendVideoMinVisitCount = 100;

                tamindirEntity.DailyAnalysisVideoGenerationLimit = 0;
                tamindirEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(tamindirEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), tamindirClientId, "haber", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.tamindir.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    tamindirClientId.ToString(),
                    tamIndirTenantId.ToString()
                );

                #endregion

                #region techno-to-day

                var technoToDayTenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717");
                var technoToDayClientId = Guid.Parse("37fbec7b-9db2-4b68-a108-56cbfd6aa9ca");
                string technoToDayDomainName = "www.technotoday.com.tr";
                var technoToDayEntity = new Client(technoToDayClientId, technoToDayTenantId, technoToDayDomainName);

                technoToDayEntity.DailyDirectVideoGenerationLimit = 0;
                technoToDayEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                technoToDayEntity.DailyTrendVideoGenerationLimit = 0;
                technoToDayEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                technoToDayEntity.DailyTrendVideoWaitStatisticHour = 2;
                technoToDayEntity.DailyTrendVideoMinVisitCount = 100;

                technoToDayEntity.DailyAnalysisVideoGenerationLimit = 0;
                technoToDayEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(technoToDayEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), technoToDayClientId, "kategori", ClientFilterTypes.ExcludeFilter));
                logger.LogDebug("{WorkerName} | www.technotoday.com.tr CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    technoToDayClientId.ToString(),
                    technoToDayTenantId.ToString()
                );

                #endregion

                #region sondakika

                var sondakikaTenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7");
                var sondakikaClientId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb");
                string sondakikaDomainName = "www.sondakika.com";
                var sondakikaEntity = new Client(sondakikaClientId, sondakikaTenantId, sondakikaDomainName);

                sondakikaEntity.DailyDirectVideoGenerationLimit = 0;
                sondakikaEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                sondakikaEntity.DailyTrendVideoGenerationLimit = 0;
                sondakikaEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                sondakikaEntity.DailyTrendVideoWaitStatisticHour = 2;
                sondakikaEntity.DailyTrendVideoMinVisitCount = 100;

                sondakikaEntity.DailyAnalysisVideoGenerationLimit = 0;
                sondakikaEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(sondakikaEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "guncel", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "dunya", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "ekonomi", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "spor", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "magazin", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "politika", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "finans", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "teknoloji", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "kultur-sanat", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "kadin", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "moda", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "otomobil", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "yasam", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "saglik", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "turizm", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "egitim", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), sondakikaClientId, "3-sayfa", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.sondakika.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    sondakikaClientId.ToString(),
                    sondakikaTenantId.ToString()
                );

                #endregion

                #region t24

                var t24TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
                var t24ClientId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901");
                string t24DomainName = "www.t24.com.tr";
                var t24Entity = new Client(t24ClientId, t24TenantId, t24DomainName);

                t24Entity.DailyDirectVideoGenerationLimit = 0;
                t24Entity.DailyDirectVideoGenerationStartedUtcHour = 0;

                t24Entity.DailyTrendVideoGenerationLimit = 0;
                t24Entity.DailyTrendVideoGenerationStartedUtcHour = 8;
                t24Entity.DailyTrendVideoWaitStatisticHour = 2;
                t24Entity.DailyTrendVideoMinVisitCount = 100;

                t24Entity.DailyAnalysisVideoGenerationLimit = 0;
                t24Entity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(t24Entity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), t24ClientId, "haber", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.t24.com.tr CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    t24ClientId.ToString(),
                    t24TenantId.ToString()
                );

                #endregion

                #region cnbce

                var cnbceTenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a");
                var cnbceClientId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e");
                string cnbceDomainName = "www.cnbce.com";
                var cnbceEntity = new Client(cnbceClientId, cnbceTenantId, cnbceDomainName);

                cnbceEntity.DailyDirectVideoGenerationLimit = 0;
                cnbceEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                cnbceEntity.DailyTrendVideoGenerationLimit = 0;
                cnbceEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                cnbceEntity.DailyTrendVideoWaitStatisticHour = 2;
                cnbceEntity.DailyTrendVideoMinVisitCount = 100;

                cnbceEntity.DailyAnalysisVideoGenerationLimit = 0;
                cnbceEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(cnbceEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "piyasalar", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "veriler", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "is-dunyasi", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "enerji", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "girisim", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "fuar", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "dijital-varliklar", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "kripto", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "savunma-sanayii", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "borsa", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "sigorta", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "teknoloji", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "haberler", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "gayrimenkul", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "otomotiv", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "gundem", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "sirket-haberleri", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "doviz", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "altin", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), cnbceClientId, "emtia", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.cnbce.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    cnbceClientId.ToString(),
                    cnbceTenantId.ToString()
                );

                #endregion

                #region box-office-turkiye

                var boxOfficeTurkiyeTenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c");
                var boxOfficeTurkiyeClientId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a");
                string boxOfficeTurkiyeDomainName = "www.boxofficeturkiye.com";
                var boxOfficeTurkiyeEntity = new Client(boxOfficeTurkiyeClientId, boxOfficeTurkiyeTenantId, boxOfficeTurkiyeDomainName);

                boxOfficeTurkiyeEntity.DailyDirectVideoGenerationLimit = 0;
                boxOfficeTurkiyeEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                boxOfficeTurkiyeEntity.DailyTrendVideoGenerationLimit = 0;
                boxOfficeTurkiyeEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                boxOfficeTurkiyeEntity.DailyTrendVideoWaitStatisticHour = 2;
                boxOfficeTurkiyeEntity.DailyTrendVideoMinVisitCount = 100;

                boxOfficeTurkiyeEntity.DailyAnalysisVideoGenerationLimit = 0;
                boxOfficeTurkiyeEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(boxOfficeTurkiyeEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), boxOfficeTurkiyeClientId, "haber", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.boxofficeturkiye.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    boxOfficeTurkiyeClientId.ToString(),
                    boxOfficeTurkiyeTenantId.ToString()
                );

                #endregion

                #region diyetkolik

                var diyetkolikTenantId = Guid.Parse("87bfc020-422f-448c-bd67-210b3b66f727");
                var diyetkolikClientId = Guid.Parse("23a70bfe-af27-490b-b936-72d2e1e5b7db");
                string diyetkolikDomainName = "www.diyetkolik.com";
                var diyetkolikEntity = new Client(diyetkolikClientId, diyetkolikTenantId, diyetkolikDomainName);

                diyetkolikEntity.DailyDirectVideoGenerationLimit = 0;
                diyetkolikEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                diyetkolikEntity.DailyTrendVideoGenerationLimit = 0;
                diyetkolikEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                diyetkolikEntity.DailyTrendVideoWaitStatisticHour = 2;
                diyetkolikEntity.DailyTrendVideoMinVisitCount = 100;

                diyetkolikEntity.DailyAnalysisVideoGenerationLimit = 0;
                diyetkolikEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(diyetkolikEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), diyetkolikClientId, "icerik", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), diyetkolikClientId, "icerik/kategori", ClientFilterTypes.ExcludeFilter));
                logger.LogDebug("{WorkerName} | www.diyetkolik.com CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    diyetkolikClientId.ToString(),
                    diyetkolikTenantId.ToString()
                );

                #endregion

                #region inStyle

                var inStyleTenantId = Guid.Parse("092f6779-ab33-4754-8d41-43e58b92bcb1");
                var inStyleClientId = Guid.Parse("5153eac5-5dd4-41b7-93ad-268eac8a948a");
                string inStyleDomainName = "www.instyle.com.tr";
                var inStyleEntity = new Client(inStyleClientId, inStyleTenantId, inStyleDomainName);

                inStyleEntity.DailyDirectVideoGenerationLimit = 0;
                inStyleEntity.DailyDirectVideoGenerationStartedUtcHour = 0;

                inStyleEntity.DailyTrendVideoGenerationLimit = 0;
                inStyleEntity.DailyTrendVideoGenerationStartedUtcHour = 8;
                inStyleEntity.DailyTrendVideoWaitStatisticHour = 2;
                inStyleEntity.DailyTrendVideoMinVisitCount = 100;

                inStyleEntity.DailyAnalysisVideoGenerationLimit = 0;
                inStyleEntity.DailyAnalysisVideoGenerationStartedUtcHour = 6;

                dbContext.Clients.Add(inStyleEntity);
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "pop-kultur", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "moda", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "guzellik-welness", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "astroloji", ClientFilterTypes.IncludeFilter));
                dbContext.ClientPathFilters.Add(new ClientPathFilter(Guid.CreateVersion7(), inStyleClientId, "kadin", ClientFilterTypes.IncludeFilter));
                logger.LogDebug("{WorkerName} | www.instyle.com.tr CLIENT DATA ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(EfCoreSeederService),
                    inStyleClientId.ToString(),
                    inStyleTenantId.ToString()
                );

                #endregion

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}