using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.Enums;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Setup;

public static class SubscriptionSeeder
{
    public static async Task SeedAsync(IdentityServiceDbContext db, IAppConsoleLogger logger, IPasswordHasher passwordHasher)
    {
        // EnsureCompanies
        var companyTechSummus = await GetOrCreateCompanyAsync(db, logger, title: "Tech Summus", name: "Tech Summus");
        var companyCiner = await GetOrCreateCompanyAsync(db, logger, title: "Ciner Medya Grubu", name: "Ciner");
        var companyIlbak = await GetOrCreateCompanyAsync(db, logger, title: "İlbak TV Yayıncılık ve İletişim A.Ş.", name: "İlbak");
        var companyVeriban = await GetOrCreateCompanyAsync(db, logger, title: "Veriban Elektronik Veri İşleme ve Saklama Hizmetleri A.Ş.", name: "Veriban");
        var companyT24 = await GetOrCreateCompanyAsync(db, logger, title: "Tempo24 Basın Yayın Prodüksiyon Ltd. Şti.", name: "Tempo 24");
        var companyTamIndir = await GetOrCreateCompanyAsync(db, logger, title: "CNT İnteraktif Bilgi Teknolojileri A.Ş.", name: "Tam İndir");
        var companySonDakika = await GetOrCreateCompanyAsync(db, logger, title: "Melon Medya A.Ş.", name: "Son Dakika");
        var companyTechnoToday = await GetOrCreateCompanyAsync(db, logger, title: "Etna Medya Hizmetleri", name: "TechnoToday");
        var companyKisaDalga = await GetOrCreateCompanyAsync(db, logger, title: "Kısa Dalga Medya Ltd. Şti.", name: "Kısa Dalga");
        var companyDunya = await GetOrCreateCompanyAsync(db, logger, title: "Dünya Grup Medya Gazetecilik A.Ş.", name: "Dünya");
        var companyBoxoffice = await GetOrCreateCompanyAsync(db, logger, title: "Demirören Medya Grubu A.Ş.", name: "BoxOffice");
        var companyDiyetKolik = await GetOrCreateCompanyAsync(db, logger, title: "PCI Yazılım Danışmanlık ve Organizasyon Ltd. Şti.", name: "Diyetkolik");
        var companyInStyle = await GetOrCreateCompanyAsync(db, logger, title: "InStyle Türkiye", name: "InStyle");

        // EnsureCustomers
        var customerTechSummus = await GetOrCreateCustomerAsync(db, logger, companyId: companyTechSummus.Id, domain: "demo.techsummus.com", customerId: Guid.Parse(CustomerIds.TechSummusCustomerId));
        var customerHaberturk = await GetOrCreateCustomerAsync(db, logger, companyId: companyCiner.Id, domain: "haberturk.com", customerId: Guid.Parse(CustomerIds.HaberturkCustomerId));
        var customerBloomberght = await GetOrCreateCustomerAsync(db, logger, companyId: companyCiner.Id, domain: "bloomberght.com", customerId: Guid.Parse(CustomerIds.BloomberghtCustomerId));
        var customerCnbce = await GetOrCreateCustomerAsync(db, logger, companyId: companyIlbak.Id, domain: "cnbce.com", customerId: Guid.Parse(CustomerIds.CnbceCustomerId));
        var customerVeribanIst = await GetOrCreateCustomerAsync(db, logger, companyId: companyVeriban.Id, domain: "veriban-ist-sube.com.tr", customerId: Guid.Parse(CustomerIds.VeribanIstCustomerId));
        var customerVeribanAnk = await GetOrCreateCustomerAsync(db, logger, companyId: companyVeriban.Id, domain: "veriban-ankara-sube.com.tr", customerId: Guid.Parse(CustomerIds.VeribanAnkCustomerId));
        var customerT24 = await GetOrCreateCustomerAsync(db, logger, companyId: companyT24.Id, domain: "t24.com.tr", customerId: Guid.Parse(CustomerIds.T24CustomerId));
        var customerTamIndir = await GetOrCreateCustomerAsync(db, logger, companyId: companyTamIndir.Id, domain: "tamindir.com", customerId: Guid.Parse(CustomerIds.TamIndirCustomerId));
        var customerSonDakika = await GetOrCreateCustomerAsync(db, logger, companyId: companySonDakika.Id, domain: "sondakika.com", customerId: Guid.Parse(CustomerIds.SonDakikaCustomerId));
        var customerTechnoToday = await GetOrCreateCustomerAsync(db, logger, companyId: companyTechnoToday.Id, domain: "technotoday.com.tr", customerId: Guid.Parse(CustomerIds.TechnoTodayCustomerId));
        var customerKisaDalga = await GetOrCreateCustomerAsync(db, logger, companyId: companyKisaDalga.Id, domain: "kisadalga.net", customerId: Guid.Parse(CustomerIds.KisaDalgaCustomerId));
        var customerDunya = await GetOrCreateCustomerAsync(db, logger, companyId: companyDunya.Id, domain: "dunya.com", customerId: Guid.Parse(CustomerIds.DunyaCustomerId));
        var customerBoxoffice = await GetOrCreateCustomerAsync(db, logger, companyId: companyBoxoffice.Id, domain: "boxofficeturkiye.com", customerId: Guid.Parse(CustomerIds.BoxofficeCustomerId));
        var customerDiyetKolik = await GetOrCreateCustomerAsync(db, logger, companyId: companyDiyetKolik.Id, domain: "diyetkolik.com", customerId: Guid.Parse(CustomerIds.DiyetKolikCustomerId));
        var customerInStyle = await GetOrCreateCustomerAsync(db, logger, companyId: companyInStyle.Id, domain: "instyle.com.tr", customerId: Guid.Parse(CustomerIds.InStyleCustomerId));

        // EnsureProductTypes
        _ = await GetOrCreateProductTypeAsync(db, logger, code: "VP", name: "Video Platform", productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        _ = await GetOrCreateProductTypeAsync(db, logger, code: "VV", name: "Vertical Video", productTypeId: Guid.Parse(ProductTypeIds.VerticalVideo));
        _ = await GetOrCreateProductTypeAsync(db, logger, code: "PC", name: "PodCast", productTypeId: Guid.Parse(ProductTypeIds.PodCast));
        _ = await GetOrCreateProductTypeAsync(db, logger, code: "AW", name: "AdWall", productTypeId: Guid.Parse(ProductTypeIds.AdWall));
        _ = await GetOrCreateProductTypeAsync(db, logger, code: "BT", name: "Bidding Tech", productTypeId: Guid.Parse(ProductTypeIds.BiddingTech));
        _ = await GetOrCreateProductTypeAsync(db, logger, code: "EI", name: "EInvoice", productTypeId: Guid.Parse(ProductTypeIds.EFatura));

        // EnsureSubscriptions
        var subAudio1 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.AudioTimeTenantId), customer: customerHaberturk, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subAudio2 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.AudioTimeTenantId), customer: customerHaberturk, productTypeId: Guid.Parse(ProductTypeIds.PodCast));
        var subAudio3 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.AudioTimeTenantId), customer: customerBloomberght, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subAudio4 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.AudioTimeTenantId), customer: customerBloomberght, productTypeId: Guid.Parse(ProductTypeIds.PodCast));
        var subAudio5 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.AudioTimeTenantId), customer: customerCnbce, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));

        var subTech1 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerCnbce, productTypeId: Guid.Parse(ProductTypeIds.PodCast));
        var subTech2 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerTechSummus, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech3 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerT24, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech4 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerTamIndir, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech5 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerSonDakika, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech6 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerTechnoToday, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech7 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerKisaDalga, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech8 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerDunya, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech9 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerBoxoffice, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech10 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerDiyetKolik, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));
        var subTech11 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.TechsummusTenantId), customer: customerInStyle, productTypeId: Guid.Parse(ProductTypeIds.VideoPlatform));

        var subEpartner1 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.EfaturaPartnerTenantId), customer: customerVeribanIst, productTypeId: Guid.Parse(ProductTypeIds.EFatura));
        var subEpartner2 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantIds.EfaturaPartnerTenantId), customer: customerVeribanAnk, productTypeId: Guid.Parse(ProductTypeIds.EFatura));

        // EnsureAppRoleSubscriptions
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleIds.AudioTimeTenantRoleId), subscriptionId: subAudio1.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleIds.AudioTimeTenantRoleId), subscriptionId: subAudio2.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleIds.AudioTimeTenantRoleId), subscriptionId: subAudio3.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleIds.AudioTimeTenantRoleId), subscriptionId: subAudio4.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleIds.AudioTimeTenantRoleId), subscriptionId: subAudio5.Id);

        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech1.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech2.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech3.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech4.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech5.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech6.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech7.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech8.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech9.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech10.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleIds.TechsummusTenantRoleId), subscriptionId: subTech11.Id);

        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.EfaturaPartnerTenantId), roleId: Guid.Parse(TenantRoleIds.EfaturaPartnerTenantRoleId), subscriptionId: subEpartner1.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantIds.EfaturaPartnerTenantId), roleId: Guid.Parse(TenantRoleIds.EfaturaPartnerTenantRoleId), subscriptionId: subEpartner2.Id);

        // update changes
        await db.SaveChangesAsync();
    }

    private static async Task<Company> GetOrCreateCompanyAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        string title, string name)
    {
        string normalizedName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(name));

        var company = await db.Companies.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

        if (company is not null)
            return company;

        company = new Company(
            id: Guid.CreateVersion7(),
            title: title,
            name: name
        );

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED COMPANY -> {CompanyName} added", nameof(EfCoreSeederService), normalizedName);

        return company;
    }

    private static async Task<Customer> GetOrCreateCustomerAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        Guid companyId, string domain, Guid? customerId)
    {
        string normalizedDomain = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(domain));

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.NormalizedDomain == normalizedDomain);

        if (customer is not null)
            return customer;

        customerId ??= Guid.CreateVersion7();

        customer = new Customer(
            id: customerId.Value,
            companyId: companyId,
            domain: domain
        );

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED CUSTOMER -> {CustomerName} added", nameof(EfCoreSeederService), normalizedDomain);

        return customer;
    }

    private static async Task<ProductType> GetOrCreateProductTypeAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        string code, string name, Guid? productTypeId)
    {
        string normalizedName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(name));

        var productType = await db.ProductTypes.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName);

        if (productType is not null)
            return productType;

        productTypeId ??= Guid.CreateVersion7();

        productType = new ProductType(
            id: productTypeId.Value,
            code: code,
            name: name
        );

        db.ProductTypes.Add(productType);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED PRODUCT_TYPE -> {ProductTypeName} added", nameof(EfCoreSeederService), normalizedName);

        return productType;
    }

    private static async Task<Subscription> GetOrCreateSubscriptionAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        Guid resellerTenantId, Customer customer, Guid productTypeId)
    {
        var subscription = await db.Subscriptions.FirstOrDefaultAsync(x =>
            x.ResellerTenantId == resellerTenantId
            && x.CompanyId == customer.CompanyId
            && x.CustomerId == customer.Id
            && x.ProductTypeId == productTypeId
        );

        if (subscription is not null)
            return subscription;

        subscription = new Subscription(
            resellerTenantId: resellerTenantId,
            companyId: customer.CompanyId,
            customerId: customer.Id,
            productTypeId: productTypeId
        );

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED SUBSCRIPTION -> {CustomerDomain}, ProductTypeId : {ProductTypeId} added",
            nameof(EfCoreSeederService),
            customer.NormalizedDomain,
            productTypeId.ToString());

        return subscription;
    }

    private static async Task<AppRoleSubscription> GetOrCreateAppRoleSubscriptionAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        Guid tenantId, Guid roleId, Guid subscriptionId)
    {
        var roleSubscription = await db.AppRoleSubscriptions.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId
            && x.RoleId == roleId
            && x.SubscriptionId == subscriptionId
        );

        if (roleSubscription is not null)
            return roleSubscription;

        roleSubscription = new AppRoleSubscription { TenantId = tenantId, RoleId = roleId, SubscriptionId = subscriptionId };

        db.AppRoleSubscriptions.Add(roleSubscription);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED ROLE_SUBSCRIPTION -> RoleId : {RoleId}, SubscriptionId : {SubscriptionId} added",
            nameof(EfCoreSeederService),
            roleId.ToString(),
            subscriptionId.ToString());

        return roleSubscription;
    }
}