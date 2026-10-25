using Abc.OrderService.Domain.OrderDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Abc.OrderService.Domain.OrderDomain.Entities;

/// <summary>
/// What a customer ordered: its lines (fixed once the order is made), its status and a note.
/// The audit fields, the soft delete and the tenant are done by the persistence (<see cref="ICreationAudited"/>, <see cref="IModificationAudited"/>, <see cref="ISoftDelete"/>,
/// <see cref="IMultiTenant"/>): the entity only keeps them.
/// </summary>
public sealed class Order : ICreationAudited, IModificationAudited, ISoftDelete, IMultiTenant
{
    private readonly List<OrderLine> _lines = [];

    // for EF Core
    private Order()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>The user name (the <c>sub</c> of the token) of the customer who made the order.</summary>
    public string CustomerUserName { get; private set; } = string.Empty;

    /// <summary>The customer's name in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedCustomerUserName { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public string? Note { get; private set; }

    public decimal TotalAmount { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

    public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

    public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);

    public static Order Create(string customerUserName, string? note, IEnumerable<OrderLine> lines)
    {
        string customer = customerUserName?.Trim() ?? string.Empty;
        if (customer.Length == 0 || customer.Length > OrderConsts.CustomerUserNameMaxLength)
        {
            throw new DomainException("An order needs a signed-in customer.");
        }

        Order order = new()
        {
            Id = Guid.CreateVersion7(),
            CustomerUserName = customer,
            NormalizedCustomerUserName = Normalize(customer),
            Status = OrderStatus.Pending,
        };
        order._lines.AddRange(lines ?? []);
        if (order._lines.Count == 0)
        {
            throw new DomainException("An order has at least one line.");
        }

        order.TotalAmount = order._lines.Sum(line => line.Total);
        order.SetNote(note);
        return order;
    }

    /// <summary>A user name as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    /// <summary>Changes the status and the note; a delivered or cancelled order is final.</summary>
    public void Update(OrderStatus status, string? note)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled && status != Status)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} order cannot change its status.");
        }

        Status = status;
        SetNote(note);
    }

    private void SetNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim().Length > OrderConsts.NoteMaxLength
            ? throw new DomainException($"Note is at most {OrderConsts.NoteMaxLength} characters.")
            : note.Trim();
    }
}
