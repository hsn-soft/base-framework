using Abc.ShipmentService.Domain.CustomerDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Abc.ShipmentService.Domain.CustomerDomain.Entities;

/// <summary>
/// Whom a parcel and its payment are for. The shipment service makes one when an order of a user it does not know yet reaches it, and keeps it: the data of the person is the shipment service's own, not the identity service's.
/// The audit fields and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class Customer : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private Customer()
    {
    }

    /// <summary>The id of the user in the identity service, when the customer is a user (a customer made by hand has none).</summary>
    public Guid? UserId { get; private set; }

    /// <summary>The user name: what finds the customer of an order.</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>The user name in capitals: what is unique, so "Ada" and "ada" are one customer.</summary>
    public string NormalizedUserName { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    /// <summary>The full name in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedFullName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>Where the parcels go by default (the address of the last order, for a customer made from an order).</summary>
    public string? Address { get; private set; }

    public Guid TenantId { get; private set; }

    public static Customer Create(string userName, string fullName, string? email, string? phone, string? address, Guid? userId = null)
    {
        Customer customer = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            UserName = Guard.NotNullOrWhiteSpace(userName, nameof(UserName), CustomerConsts.UserNameMaxLength).Trim(),
        };
        customer.NormalizedUserName = StringHelper.Normalize(customer.UserName);
        customer.Update(fullName, email, phone, address);
        return customer;
    }

    public void Update(string fullName, string? email, string? phone, string? address)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, nameof(FullName), CustomerConsts.FullNameMaxLength).Trim();
        NormalizedFullName = StringHelper.Normalize(FullName);
        Email = Optional(email, nameof(Email), CustomerConsts.EmailMaxLength);
        Phone = Optional(phone, nameof(Phone), CustomerConsts.PhoneMaxLength);
        Address = Optional(address, nameof(Address), CustomerConsts.AddressMaxLength);
    }

    private static string? Optional(string? value, string name, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Guard.Length(value.Trim(), name, maxLength);
}
