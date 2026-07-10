using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Constants;
using Hhs.Shared.Helper.Enums;
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
        var customerTechSummus = await GetOrCreateCustomerAsync(db, logger, companyId: companyTechSummus.Id, domain: "demo.techsummus.com", customerId: Guid.Parse(CustomerSeedIds.TechSummusCustomerId));
        var customerHaberturk = await GetOrCreateCustomerAsync(db, logger, companyId: companyCiner.Id, domain: "haberturk.com", customerId: Guid.Parse(CustomerSeedIds.HaberturkCustomerId));
        var customerBloomberght = await GetOrCreateCustomerAsync(db, logger, companyId: companyCiner.Id, domain: "bloomberght.com", customerId: Guid.Parse(CustomerSeedIds.BloomberghtCustomerId));
        var customerCnbce = await GetOrCreateCustomerAsync(db, logger, companyId: companyIlbak.Id, domain: "cnbce.com", customerId: Guid.Parse(CustomerSeedIds.CnbceCustomerId));
        var customerVeribanIst = await GetOrCreateCustomerAsync(db, logger, companyId: companyVeriban.Id, domain: "veriban-ist-sube.com.tr", customerId: Guid.Parse(CustomerSeedIds.VeribanIstCustomerId));
        var customerVeribanAnk = await GetOrCreateCustomerAsync(db, logger, companyId: companyVeriban.Id, domain: "veriban-ankara-sube.com.tr", customerId: Guid.Parse(CustomerSeedIds.VeribanAnkCustomerId));
        var customerT24 = await GetOrCreateCustomerAsync(db, logger, companyId: companyT24.Id, domain: "t24.com.tr", customerId: Guid.Parse(CustomerSeedIds.T24CustomerId));
        var customerTamIndir = await GetOrCreateCustomerAsync(db, logger, companyId: companyTamIndir.Id, domain: "tamindir.com", customerId: Guid.Parse(CustomerSeedIds.TamIndirCustomerId));
        var customerSonDakika = await GetOrCreateCustomerAsync(db, logger, companyId: companySonDakika.Id, domain: "sondakika.com", customerId: Guid.Parse(CustomerSeedIds.SonDakikaCustomerId));
        var customerTechnoToday = await GetOrCreateCustomerAsync(db, logger, companyId: companyTechnoToday.Id, domain: "technotoday.com.tr", customerId: Guid.Parse(CustomerSeedIds.TechnoTodayCustomerId));
        var customerKisaDalga = await GetOrCreateCustomerAsync(db, logger, companyId: companyKisaDalga.Id, domain: "kisadalga.net", customerId: Guid.Parse(CustomerSeedIds.KisaDalgaCustomerId));
        var customerDunya = await GetOrCreateCustomerAsync(db, logger, companyId: companyDunya.Id, domain: "dunya.com", customerId: Guid.Parse(CustomerSeedIds.DunyaCustomerId));
        var customerBoxoffice = await GetOrCreateCustomerAsync(db, logger, companyId: companyBoxoffice.Id, domain: "boxofficeturkiye.com", customerId: Guid.Parse(CustomerSeedIds.BoxofficeCustomerId));
        var customerDiyetKolik = await GetOrCreateCustomerAsync(db, logger, companyId: companyDiyetKolik.Id, domain: "diyetkolik.com", customerId: Guid.Parse(CustomerSeedIds.DiyetKolikCustomerId));
        var customerInStyle = await GetOrCreateCustomerAsync(db, logger, companyId: companyInStyle.Id, domain: "instyle.com.tr", customerId: Guid.Parse(CustomerSeedIds.InStyleCustomerId));

        // EnsureSubscriptions
        var subAudio1 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), customer: customerHaberturk, productType: ProductTypes.VideoPlatform);
        var subAudio2 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), customer: customerHaberturk, productType: ProductTypes.Podcast);
        var subAudio3 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), customer: customerBloomberght, productType: ProductTypes.VideoPlatform);
        var subAudio4 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), customer: customerBloomberght, productType: ProductTypes.Podcast);
        var subAudio5 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), customer: customerCnbce, productType: ProductTypes.VideoPlatform);

        var subTech1 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerCnbce, productType: ProductTypes.Podcast);
        var subTech2 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerTechSummus, productType: ProductTypes.VideoPlatform);
        var subTech3 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerT24, productType: ProductTypes.VideoPlatform);
        var subTech4 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerTamIndir, productType: ProductTypes.VideoPlatform);
        var subTech5 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerSonDakika, productType: ProductTypes.VideoPlatform);
        var subTech6 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerTechnoToday, productType: ProductTypes.VideoPlatform);
        var subTech7 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerKisaDalga, productType: ProductTypes.VideoPlatform);
        var subTech8 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerDunya, productType: ProductTypes.VideoPlatform);
        var subTech9 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerBoxoffice, productType: ProductTypes.VideoPlatform);
        var subTech10 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerDiyetKolik, productType: ProductTypes.VideoPlatform);
        var subTech11 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), customer: customerInStyle, productType: ProductTypes.VideoPlatform);

        var subEpartner1 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.EfaturaPartnerTenantId), customer: customerVeribanIst, productType: ProductTypes.EInvoice);
        var subEpartner2 = await GetOrCreateSubscriptionAsync(db, logger, resellerTenantId: Guid.Parse(TenantSeedIds.EfaturaPartnerTenantId), customer: customerVeribanAnk, productType: ProductTypes.EInvoice);

        // EnsureAppRoleSubscriptions
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleSeedIds.AudioTimeTenantRoleId), subscriptionId: subAudio1.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleSeedIds.AudioTimeTenantRoleId), subscriptionId: subAudio2.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleSeedIds.AudioTimeTenantRoleId), subscriptionId: subAudio3.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleSeedIds.AudioTimeTenantRoleId), subscriptionId: subAudio4.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.AudioTimeTenantId), roleId: Guid.Parse(TenantRoleSeedIds.AudioTimeTenantRoleId), subscriptionId: subAudio5.Id);

        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech1.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech2.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech3.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech4.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech5.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech6.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech7.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech8.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech9.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech10.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.TechsummusTenantId), roleId: Guid.Parse(TenantRoleSeedIds.TechsummusTenantRoleId), subscriptionId: subTech11.Id);

        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.EfaturaPartnerTenantId), roleId: Guid.Parse(TenantRoleSeedIds.EfaturaPartnerTenantRoleId), subscriptionId: subEpartner1.Id);
        _ = await GetOrCreateAppRoleSubscriptionAsync(db, logger, tenantId: Guid.Parse(TenantSeedIds.EfaturaPartnerTenantId), roleId: Guid.Parse(TenantRoleSeedIds.EfaturaPartnerTenantRoleId), subscriptionId: subEpartner2.Id);

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

    private static async Task<Subscription> GetOrCreateSubscriptionAsync(IdentityServiceDbContext db, IAppConsoleLogger logger,
        Guid resellerTenantId, Customer customer, ProductTypes productType)
    {
        var subscription = await db.Subscriptions.FirstOrDefaultAsync(x =>
            x.ResellerTenantId == resellerTenantId
            && x.CompanyId == customer.CompanyId
            && x.CustomerId == customer.Id
            && x.ProductType == productType
        );

        if (subscription is not null)
            return subscription;

        subscription = new Subscription(
            resellerTenantId: resellerTenantId,
            companyId: customer.CompanyId,
            customerId: customer.Id,
            productType: productType
        );

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED SUBSCRIPTION -> {CustomerDomain}, ProductTypeId : {ProductTypeId} added",
            nameof(EfCoreSeederService),
            customer.NormalizedDomain,
            productType.ToString());

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