using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Providers;
using Hhs.Shared.Helper.Utils;
using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.MongoDb.Setup;

public static class CustomerVpSettingSeeder
{
    public static async Task SeedAsync(TextNormalizerServiceDbContext db, IAppConsoleLogger logger)
    {
        #region scenarious

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario01CustomerId),
            domainName: "tst01.scenario.com",
            outlineProviderKey: ProviderKeys.OutlineFast,
            analysisOutlineContentPrompt: "test outline content prompt",
            analysisOutlineIntroPrompt: "test outline intro prompt",
            analysisOutlineOutroPrompt: "test outro prompt",
            isScrapingOperationActive: false
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario02CustomerId),
            domainName: "tst02.scenario.com",
            outlineProviderKey: ProviderKeys.OutlineFast,
            analysisOutlineContentPrompt: "test outline content prompt",
            analysisOutlineIntroPrompt: "test outline intro prompt",
            analysisOutlineOutroPrompt: "test outro prompt",
            isScrapingOperationActive: false
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario03CustomerId),
            domainName: "tst03.scenario.com",
            outlineProviderKey: ProviderKeys.OutlineQueue,
            analysisOutlineContentPrompt: "test outline content prompt",
            analysisOutlineIntroPrompt: "test outline intro prompt",
            analysisOutlineOutroPrompt: "test outro prompt",
            isScrapingOperationActive: false
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario04CustomerId),
            domainName: "tst04.scenario.com",
            outlineProviderKey: ProviderKeys.OutlineQueue,
            analysisOutlineContentPrompt: "test outline content prompt",
            analysisOutlineIntroPrompt: "test outline intro prompt",
            analysisOutlineOutroPrompt: "test outro prompt",
            isScrapingOperationActive: false
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario05CustomerId),
            domainName: "tst05.scenario.com",
            outlineProviderKey: ProviderKeys.OutlineFast,
            analysisOutlineContentPrompt: "test outline content prompt",
            analysisOutlineIntroPrompt: "test outline intro prompt",
            analysisOutlineOutroPrompt: "test outro prompt",
            isScrapingOperationActive: false
        );

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.Scenario06CustomerId),
            domainName: "tst06.scenario.com",
            outlineProviderKey: ProviderKeys.OutlineQueue,
            analysisOutlineContentPrompt: "test outline content prompt",
            analysisOutlineIntroPrompt: "test outline intro prompt",
            analysisOutlineOutroPrompt: "test outro prompt",
            isScrapingOperationActive: false
        );

        #endregion

        #region demo-techsummus

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechSummusCustomerId),
            domainName: "demo.techsummus.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        #region t24

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.T24CustomerId),
            domainName: "t24.com.tr",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        #region tam-indir

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TamIndirCustomerId),
            domainName: "tamindir.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        #region sondakika

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.SonDakikaCustomerId),
            domainName: "sondakika.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı SONDAKIKA.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin SONDAKIKA.com'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için SONDAKIKA.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region techno-to-day

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.TechnoTodayCustomerId),
            domainName: "technotoday.com.tr",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        #region kisa-dalga

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.KisaDalgaCustomerId),
            domainName: "kisadalga.net",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı KISADALGA.net. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin KISADALGA.net'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için KISADALGA.net'in takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region dunya

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DunyaCustomerId),
            domainName: "dunya.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı DÜNYA.com. " +
                                          "Ekonomi haberleri içeren bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin DÜNYA.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için DÜNYA.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region box-office-turkiye

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BoxofficeCustomerId),
            domainName: "boxofficeturkiye.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı boxofficeturkiye.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin boxofficeturkiye.com'da paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için boxofficeturkiye.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region diyetkolik

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.DiyetKolikCustomerId),
            domainName: "diyetkolik.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.InStyleCustomerId),
            domainName: "instyle.com.tr",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        #region haberturk

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.HaberturkCustomerId),
            domainName: "haberturk.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı HABERTURK.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin HABERTURK.com'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için HABERTURK.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region bloomberght

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.BloomberghtCustomerId),
            domainName: "bloomberght.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
            analysisOutlineContentPrompt: "Merhaba, sırasıyla sana haber başlıkları vereceğim. Haberlerin sunulduğu sitenin adı BLOOMBERGHT.com. " +
                                          "Bu haber sitesinde, başlık içeriklerini değiştirmeden, günün öne çıkan bilgilerinin BLOOMBERGHT.com'de paylaşılacağına dair bir giriş cümlesi eklemeni, " +
                                          "başlıklar arası geçişleri bir haber spikeri gibi sunup, sonunda gündemi takip etmek için BLOOMBERGHT.com'un takip edilmesi gerektiğine dair bir cümle eklemeni istiyorum.",
            analysisOutlineIntroPrompt: "",
            analysisOutlineOutroPrompt: ""
        );

        #endregion

        #region cnbce

        await GetOrCreateCustomerVpSettingAsync(db, logger,
            customerId: Guid.Parse(CustomerSeedIds.CnbceCustomerId),
            domainName: "cnbce.com",
            outlineProviderKey: ProviderKeys.OutlineOpenAi,
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

        logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR CONFIG", nameof(MongoSeederService));
    }

    private static async Task GetOrCreateCustomerVpSettingAsync(TextNormalizerServiceDbContext db, IAppConsoleLogger logger,
        Guid customerId, string domainName, string outlineProviderKey,
        string analysisOutlineContentPrompt, string analysisOutlineIntroPrompt, string analysisOutlineOutroPrompt,
        string contentOutlinePrompt = null, bool isScrapingOperationActive = true)
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

        var newEntity = new CustomerVpSetting(
            id: Guid.CreateVersion7(),
            customerId: customerId,
            domainName: domainName,
            outlineProviderKey: outlineProviderKey
        )
        {
            IsScrapingOperationActive = isScrapingOperationActive,
            IsOutlineOperationActive = true,
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
            AnalysisOutlineContentPrompt = analysisOutlineContentPrompt,
            AnalysisOutlineIntroPrompt = analysisOutlineIntroPrompt,
            AnalysisOutlineOutroPrompt = analysisOutlineOutroPrompt,
            IsForceContentDetailInAnalyseActive = domainName.Equals("www.tamindir.com")
        };

        await db.CustomerVpSettings.InsertOneAsync(newEntity);

        logger.LogDebug("{WorkerName} | {DomainName} CUSTOMER_VP_SETTING DATA ADDED: scopeKey [ {scopeKey} ]", nameof(MongoSeederService),
            normaizedDomainName,
            scopeKey
        );
    }
}