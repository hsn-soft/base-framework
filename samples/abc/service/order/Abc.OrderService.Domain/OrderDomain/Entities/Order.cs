using Abc.OrderService.Domain.OrderDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Abc.OrderService.Domain.OrderDomain.Entities;

/// <summary>
/// What a customer ordered: its lines (fixed once the order is made), its status and a note.
/// The audit fields and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class Order : FullAuditedEntity<Guid>, IMultiTenant
{
    private readonly List<OrderLine> _lines = [];

    // for EF Core
    private Order()
    {
    }

    /// <summary>The user name (the <c>sub</c> of the token) of the customer who made the order.</summary>
    public string CustomerUserName { get; private set; } = string.Empty;

    /// <summary>The customer's name in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedCustomerUserName { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public string? Note { get; private set; }

    public decimal TotalAmount { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public Guid TenantId { get; private set; }

    public static Order Create(string customerUserName, string? note, IEnumerable<OrderLine> lines)
    {
        string customer = customerUserName?.Trim() ?? string.Empty;
        if (customer.Length == 0 || customer.Length > OrderConsts.CustomerUserNameMaxLength)
        {
            throw new DomainException("An order needs a signed-in customer.").WithKey("order_customer_required");
        }

        Order order = new()
        {
            Id = Guid.CreateVersion7(),
            CustomerUserName = customer,
            NormalizedCustomerUserName = StringHelper.Normalize(customer),
            Status = OrderStatus.Pending,
        };
        order._lines.AddRange(lines ?? []);
        if (order._lines.Count == 0)
        {
            throw new DomainException("An order has at least one line.").WithKey("order_lines_required");
        }

        order.TotalAmount = order._lines.Sum(line => line.Total);
        order.SetNote(note);
        return order;
    }

    /// <summary>Changes the status and the note; a delivered or cancelled order is final.</summary>
    public void Update(OrderStatus status, string? note)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled && status != Status)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} order cannot change its status.").WithKey("order_status_final", Status.ToString());
        }

        Status = status;
        SetNote(note);
    }

    private void SetNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : Guard.Length(note.Trim(), nameof(Note), OrderConsts.NoteMaxLength);
    }
}
