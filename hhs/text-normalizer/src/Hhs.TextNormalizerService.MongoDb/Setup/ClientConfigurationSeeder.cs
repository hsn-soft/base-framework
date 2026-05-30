using Hhs.TextNormalizerService.Domain.CustomerDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Logging.Abstracts;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.MongoDb.Setup;

public static class ClientConfigurationSeeder
{
    public static async Task SeedAsync(TextNormalizerServiceDbContext db, IAppConsoleLogger logger)
    {
        #region reseller tenants

        var gazeteTenantId = Guid.Parse("C35FD197-E1FE-455D-B118-38404A69931E");
        var gazeteClientId = Guid.Parse("0b16bed1-ffdb-4066-a33d-c6ee074b72e7");
        string gazeteDomainName = "test.gazete.com";
        await GetOrCreateClientConfigurationAsync(db, logger, gazeteTenantId, gazeteClientId, gazeteDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı test.gazete.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin techsummus.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için techsummus.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        var sporTenantId = Guid.Parse("B109FAF7-5AA2-451C-AC3A-51682D6C06E6");
        var sporClientId = Guid.Parse("cc16bed1-ffdb-4066-a33d-c6ee074b72aa");
        string sporDomainName = "test.spor.com";
        await GetOrCreateClientConfigurationAsync(db, logger, sporTenantId, sporClientId, sporDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı test.spor.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin techsummus.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için techsummus.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region demo-techsummus

        var demoTechSummusTenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede");
        var demoTechSummusClientId = Guid.Parse("4fe789ab-0652-4e7b-bd35-07019058081d");
        string demoTechSummusDomainName = "demo.techsummus.com";
        await GetOrCreateClientConfigurationAsync(db, logger, demoTechSummusTenantId, demoTechSummusClientId, demoTechSummusDomainName,
            analysisOutlineContentPrompt: "Sen bir haber sitesinin anchorman'i olarak çalışıyorsun. " +
                                          "Sana teknoloji ile alakalı haber başlığı ve kısa bir içerik veriyorum ve " +
                                          "sen Bu içeriği haber sitesinde sunulmak üzere 3 cümle ile yeniden yorumluyorsun. Haber şu şekilde. ",
            analysisOutlineIntroPrompt: "Merhaba, ben günün öne çıkan haberlerini gösteren bir video içeriği hazırlıyorum. " +
                                        "Bu video içeriğinin başlangıcı için bir açılış cümlesine ihtiyacım var. " +
                                        "Vereceğim bilgilere göre, bana açılış cümlesi oluşturabilir misin? " +
                                        "İçeriklerin yer aldığı site bir haber sitesi ve adı teksummusnoktakom. " +
                                        "Günlük olarak, güncel haberlerinin yer aldığı bu site Türkçe içerik üretiyor.",
            analysisOutlineOutroPrompt: "Merhaba, ben günün öne çıkan haberlerini gösteren bir video içeriği hazırlıyorum. " +
                                        "Bu video içeriğinin sonu için bir kapanış cümlesine ihtiyacım var. " +
                                        "Vereceğim bilgilere göre, bana kapanış cümlesi oluşturabilir misin? " +
                                        "İçeriklerin yer aldığı site bir haber sitesi ve adı teksummusnoktakom. " +
                                        "Günlük olarak, güncel haberlerinin yer aldığı bu site Türkçe içerik üretiyor."
        );

        #endregion

        #region dunya

        var dunyaTenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5");
        var dunyaClientId = Guid.Parse("07ca0b44-c052-4970-b128-b7b5feb1a03b");
        string dunyaDomainName = "www.dunya.com";
        await GetOrCreateClientConfigurationAsync(db, logger, dunyaTenantId, dunyaClientId, dunyaDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı DÜNYA.com. " +
                                          "Ekonomi haberleri içeren bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin DÜNYA.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için DÜNYA.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region kisa-dalga

        var kisaDalgaTenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395");
        var kisaDalgaClientId = Guid.Parse("e8265c9c-e52c-4daa-8dbe-30874e5fc02c");
        string kisaDalgaDomainName = "www.kisadalga.net";
        await GetOrCreateClientConfigurationAsync(db, logger, kisaDalgaTenantId, kisaDalgaClientId, kisaDalgaDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı KISADALGA.net. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin KISADALGA.net'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için KISADALGA.net'in takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region tam-indir

        var tamIndirTenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17");
        var tamindirClientId = Guid.Parse("18bf822c-175c-4e2d-b9e0-1c3f0b2fe013");
        string tamindirDomainName = "www.tamindir.com";
        await GetOrCreateClientConfigurationAsync(db, logger, tamIndirTenantId, tamindirClientId, tamindirDomainName,
            analysisOutlineContentPrompt: "Sen bir teknoloji sitesinin anchorman'i olarak çalışıyorsun. " +
                                          "Sana ilgili teknoloji haberinin başlığını ve içeriğini sağlıyorum. " +
                                          "sen Bu içeriği bu sitede sunulmak üzere 3 cümle ile yeniden yorumluyorsun. " +
                                          "Haber şu şekilde; ",
            analysisOutlineIntroPrompt: "\"tam-indir.com\" adlı teknoloji üzerine günlük haberlerin yer aldığı teknoloji haber " +
                                        "sitesi için en çok okunan haberlerin yer aldığı video içeriği için açılış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            analysisOutlineOutroPrompt: "\"tam-indir.com\" adlı teknoloji üzerine günlük haberlerin yer aldığı teknoloji haber " +
                                        "sitesi için en çok okunan haberlerin yer aldığı video içeriği için kapanış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            contentOutlinePrompt: "Could you please summarize the content in Turkish with 5 sentences?"
        );

        #endregion

        #region techno-to-day

        var technoToDayTenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717");
        var technoToDayClientId = Guid.Parse("37fbec7b-9db2-4b68-a108-56cbfd6aa9ca");
        string technoToDayDomainName = "www.technotoday.com.tr";
        await GetOrCreateClientConfigurationAsync(db, logger, technoToDayTenantId, technoToDayClientId, technoToDayDomainName,
            analysisOutlineContentPrompt: "Sen bir haber sitesinin anchorman'i olarak çalışıyorsun. " +
                                          "Sana teknoloji ile alakalı haber başlığı ve kısa bir içerik veriyorum ve " +
                                          "sen Bu içeriği haber sitesinde sunulmak üzere 3 cümle ile yeniden yorumluyorsun. " +
                                          "Haber şu şekilde. ",
            analysisOutlineIntroPrompt: "Merhaba, ben günün öne çıkan haberlerini gösteren bir video içeriği hazırlıyorum. " +
                                        "Bu video içeriğinin başlangıcı için bir açılış cümlesine ihtiyacım var. " +
                                        "Vereceğim bilgilere göre, bana açılış cümlesi oluşturabilir misin? " +
                                        "İçeriklerin yer aldığı site bir teknoloji haber sitesi ve adı teknotudeynoktakom. " +
                                        "Günlük olarak, teknolojileri haberlerinin yer aldığı bu site Türkçe içerik üretiyor.",
            analysisOutlineOutroPrompt: "Merhaba, ben günün öne çıkan haberlerini gösteren bir video içeriği hazırlıyorum. " +
                                        "Bu video içeriğinin sonu için bir kapanış cümlesine ihtiyacım var. " +
                                        "Vereceğim bilgilere göre, bana kapanış cümlesi oluşturabilir misin? " +
                                        "İçeriklerin yer aldığı site bir teknoloji haber sitesi ve adı teknotudeynoktakom. " +
                                        "Günlük olarak, teknolojileri haberlerinin yer aldığı bu site Türkçe içerik üretiyor."
        );

        #endregion

        #region sondakika

        var sondakikaTenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7");
        var sondakikaClientId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb");
        string sondakikaDomainName = "www.sondakika.com";
        await GetOrCreateClientConfigurationAsync(db, logger, sondakikaTenantId, sondakikaClientId, sondakikaDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı SONDAKIKA.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin SONDAKIKA.com'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için SONDAKIKA.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region t24

        var t24TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
        var t24ClientId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901");
        string t24DomainName = "www.t24.com.tr";
        await GetOrCreateClientConfigurationAsync(db, logger, t24TenantId, t24ClientId, t24DomainName,
            analysisOutlineContentPrompt: "Sen bir haber sitesinin anchorman'i olarak çalışıyorsun. " +
                                          "Sana ilgili haberin başlığını ve içeriğini sağlıyorum. " +
                                          "sen Bu içeriği haber sitesinde sunulmak üzere 3 cümle ile yeniden yorumluyorsun. " +
                                          "Haber şu şekilde; ",
            analysisOutlineIntroPrompt: "t24.com.tr adlı Dünya ve Türkiye gündeminden günlük haberlerin yer aldığı haber " +
                                        "sitesi için en çok okunan haberlerin yer aldığı video içeriği için açılış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            analysisOutlineOutroPrompt: "t24.com.tr adlı Dünya ve Türkiye gündeminden günlük haberlerin yer aldığı haber " +
                                        "sitesi için en çok okunan haberlerin yer aldığı video içeriği için kapanış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            contentOutlinePrompt: "Could you please summarize the content in Turkish with 5 sentences?"
        );

        #endregion

        #region cnbce

        var cnbceTenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a");
        var cnbceClientId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e");
        string cnbceDomainName = "www.cnbce.com";
        await GetOrCreateClientConfigurationAsync(db, logger, cnbceTenantId, cnbceClientId, cnbceDomainName,
            analysisOutlineContentPrompt: "Sen bir haber sitesinin anchorman'i olarak çalışıyorsun. " +
                                          "Sana ilgili haberin başlığını ve içeriğini sağlıyorum. " +
                                          "sen Bu içeriği haber sitesinde sunulmak üzere 3 cümle ile yeniden yorumluyorsun. " +
                                          "Haber şu şekilde; ",
            analysisOutlineIntroPrompt: "\"cnbc-e.com\" adlı Dünya ve Türkiye Finans gündemiyle alakalı güncel haberlerin yer aldığı finans haber " +
                                        "sitesi için en çok okunan haberlerin yer aldığı video içeriği için açılış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            analysisOutlineOutroPrompt: "\"cnbc-e.com\" adlı Dünya ve Türkiye Finans gündemiyle alakalı güncel haberlerin yer aldığı finans haber " +
                                        "sitesi için en çok okunan haberlerin yer aldığı video içeriği için kapanış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            contentOutlinePrompt: "Could you please summarize the content in Turkish with 5 sentences?"
        );

        #endregion

        #region box-office-turkiye

        var boxOfficeTurkiyeTenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c");
        var boxOfficeTurkiyeClientId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a");
        string boxOfficeTurkiyeDomainName = "www.boxofficeturkiye.com";
        await GetOrCreateClientConfigurationAsync(db, logger, boxOfficeTurkiyeTenantId, boxOfficeTurkiyeClientId, boxOfficeTurkiyeDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı boxofficeturkiye.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin boxofficeturkiye.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için boxofficeturkiye.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region diyetkolik

        var diyetkolikTenantId = Guid.Parse("87bfc020-422f-448c-bd67-210b3b66f727");
        var diyetkolikClientId = Guid.Parse("23a70bfe-af27-490b-b936-72d2e1e5b7db");
        string diyetkolikDomainName = "www.diyetkolik.com";
        await GetOrCreateClientConfigurationAsync(db, logger, diyetkolikTenantId, diyetkolikClientId, diyetkolikDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana içerikler sağlayacağım. Bu içeriklerin yer aldığı websitenin adı diyetkolik.com. " +
                                          "İçerik başlıklarını değiştirmeden, günün öne çıkan içeriklerin diyetkolik.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri yaptıktan sonra, en sonunda diyet ve sağlık içerikleri için diyetkolik.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "\"diyetkolik.com\" adlı Sağlıklı Yaşam içeriklerinin yer aldığı sağlık ve diyet üzerine yayın yapan " +
                                        "web sitesi için en çok okunan içeriklerin yer alacağı video tabanlı media için açılış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            analysisOutlineOutroPrompt: "\"diyetkolik.com\" adlı Sağlıklı Yaşam içeriklerinin yer aldığı sağlık ve diyet üzerine yayın yapan " +
                                        "web sitesi için en çok okunan içeriklerin yer alacağı video tabanlı media için kapanış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder"
        );

        #endregion

        #region inStyle

        var inStyleTenantId = Guid.Parse("092f6779-ab33-4754-8d41-43e58b92bcb1");
        var inStyleClientId = Guid.Parse("5153eac5-5dd4-41b7-93ad-268eac8a948a");
        string inStyleDomainName = "www.instyle.com.tr";
        await GetOrCreateClientConfigurationAsync(db, logger, inStyleTenantId, inStyleClientId, inStyleDomainName,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana içerikler sağlayacağım. Bu içeriklerin yer aldığı websitenin adı instyle.com.tr " +
                                          "İçerik başlıklarını değiştirmeden, günün öne çıkan içeriklerinin instyle.com.tr'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri yaptıktan sonra, en sonunda moda ve lifestyle içerikleri için instyle.com.tr'nin takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "\"instyle.com.tr\" adlı Lifestyle içeriklerin yer aldığı kadın güzelliği ve modası üzerine yayın yapan " +
                                        "web sitesi için en çok okunan içeriklerin yer alacağı video tabanlı media için açılış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder",
            analysisOutlineOutroPrompt: "\"instyle.com.tr\" adlı Lifestyle içeriklerin yer aldığı kadın güzelliği ve modası üzerine yayın yapan " +
                                        "web sitesi için en çok okunan içeriklerin yer alacağı video tabanlı media için kapanış cümlesi oluştur. " +
                                        "Direkt son cevabı gönder"
        );

        #endregion

        logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR CONFIG", nameof(MongoSeederService));
    }

    private static async Task GetOrCreateClientConfigurationAsync(TextNormalizerServiceDbContext db, IAppConsoleLogger logger, Guid tenantId, Guid clientId, string domainName,
        string analysisOutlineContentPrompt, string analysisOutlineIntroPrompt, string analysisOutlineOutroPrompt, string contentOutlinePrompt = null)
    {
        var filter = Builders<CustomerConfiguration>.Filter.Eq(doc => doc.ClientId, clientId);

        long customerConfigurationsCount = await db.CustomerConfigurations.CountDocumentsAsync(filter);
        if (customerConfigurationsCount > 0)
        {
            return;
        }

        var newEntity = new CustomerConfiguration(
            id: Guid.CreateVersion7(),
            tenantId,
            clientId,
            clientName: domainName,
            normalizerProvider: "ScrapingSettings",
            normalizerSetting: new ClientNormalizerSetting
            {
                IsScrapingOperationActive = true,
                IsOutlineOperationActive = true,
                ContentOutlineProvider = TextNormalizeProviderTypes.OPEN_AI,
                ContentOutlinePrompt = string.IsNullOrWhiteSpace(contentOutlinePrompt)
                    ? """
                      Sen deneyimli bir haber editörüsün. Sana bir haber metni verilecek.
                      Aşağıdaki alanları oluştur ve sadece geçerli JSON formatında döndür:
                      - Category: En fazla 3
                      - Tags: En fazla 10
                      - Spot: Yaklaşık 20 kelimeden oluşan Haber alt metni
                      - Title: Çarpıcı Haber başlığı
                      - Summary: Yaklaşık 50 kelimeden oluşan Haber Özeti
                      """
                    : contentOutlinePrompt,
                AnalysisOutlineProvider = TextNormalizeProviderTypes.OPEN_AI,
                AnalysisOutlineContentPrompt = analysisOutlineContentPrompt,
                AnalysisOutlineIntroPrompt = analysisOutlineIntroPrompt,
                AnalysisOutlineOutroPrompt = analysisOutlineOutroPrompt,
                IsForceContentDetailInAnalyseActive = domainName.Equals("www.tamindir.com")
            });

        await db.CustomerConfigurations.InsertOneAsync(newEntity);

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER CONFIGURATION ADDED: ClientId [ {ClientId} ], TenantId [ {TenantId} ]", nameof(MongoSeederService),
            domainName,
            clientId.ToString(),
            tenantId.ToString()
        );
    }
}