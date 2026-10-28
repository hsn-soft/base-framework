namespace Edx.Shared.Helper;

/// <summary>
/// What a tenant is in the e-invoice system: the taxpayer it is (its VKN, title, tax office and address, which the invoices it issues say of their supplier) and the two addresses (aliases) it has with this integrator: the sender unit (<c>gb</c>) its invoices go out from and the post box (<c>pk</c>)
/// the invoices for it come in at. A real integrator keeps them in the account of the customer; the sample's three tenants are fixed, like their ids, so every service knows them. The mock center of the sample is told the same in its settings (<c>Participants</c>).
/// </summary>
public sealed record EInvoiceProfile(
    Guid TenantId,
    string TenantCode,
    string RegisterNumber,
    string Title,
    string TaxOffice,
    string District,
    string City,
    string Country,
    string SenderUnitAlias,
    string PostBoxAlias);

public static class EdxEInvoiceProfiles
{
    public static readonly EInvoiceProfile System = new(
        EdxTenants.System, EdxTenants.SystemCode, "1234567800", "Edx Sistem Yazilim A.S.", "Kadikoy", "Kadikoy", "Istanbul", "Türkiye", "urn:mail:system_gb@edx.example", "urn:mail:system_pk@edx.example");

    public static readonly EInvoiceProfile Veriban = new(
        EdxTenants.Veriban, EdxTenants.VeribanCode, "1234567801", "Veriban Yazilim A.S.", "Kozyatagi", "Kadikoy", "Istanbul", "Türkiye", "urn:mail:veriban_gb@veriban.example", "urn:mail:veriban_pk@veriban.example");

    public static readonly EInvoiceProfile Bilgera = new(
        EdxTenants.Bilgera, EdxTenants.BilgeraCode, "1234567802", "Bilgera Bilisim A.S.", "Cankaya", "Cankaya", "Ankara", "Türkiye", "urn:mail:bilgera_gb@bilgera.example", "urn:mail:bilgera_pk@bilgera.example");

    public static IReadOnlyList<EInvoiceProfile> All { get; } = [System, Veriban, Bilgera];

    public static EInvoiceProfile? ForTenant(Guid tenantId) => All.FirstOrDefault(profile => profile.TenantId == tenantId);

    /// <summary>The tenant that has the post box (an invoice sent to this alias is the tenant's).</summary>
    public static EInvoiceProfile? ForPostBox(string alias) => All.FirstOrDefault(profile => string.Equals(profile.PostBoxAlias, alias, StringComparison.Ordinal));

    /// <summary>The tenant that has the sender unit (a system response sent to this alias is about an invoice of the tenant).</summary>
    public static EInvoiceProfile? ForSenderUnit(string alias) => All.FirstOrDefault(profile => string.Equals(profile.SenderUnitAlias, alias, StringComparison.Ordinal));

    public static EInvoiceProfile? ForRegisterNumber(string registerNumber) => All.FirstOrDefault(profile => profile.RegisterNumber == registerNumber);
}
