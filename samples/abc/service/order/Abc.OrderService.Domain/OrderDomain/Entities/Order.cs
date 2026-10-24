using Abc.OrderService.Domain.OrderDomain.Consts;
using HsnSoft.Base;

namespace Abc.OrderService.Domain.OrderDomain.Entities;

/// <summary>What a customer ordered: its lines (fixed once the order is made), its status and a note.</summary>
public sealed class Order
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

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The user name in the <c>sub</c> claim of the token of who made the order; <see langword="null"/> when nobody signed in did.</summary>
    public string? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The same for the last change.</summary>
    public string? UpdatedBy { get; private set; }

    public static Order Create(string customerUserName, string? note, IEnumerable<OrderLine> lines, DateTimeOffset now, string? by)
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
            CreatedAt = now,
            CreatedBy = Author(by),
        };
        order._lines.AddRange(lines ?? []);
        if (order._lines.Count == 0)
        {
            throw new DomainException("An order has at least one line.");
        }

        order.TotalAmount = order._lines.Sum(line => line.Total);
        order.Touch(note, now, by);
        return order;
    }

    /// <summary>A user name as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    /// <summary>Changes the status and the note; a delivered or cancelled order is final.</summary>
    public void Update(OrderStatus status, string? note, DateTimeOffset now, string? by)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled && status != Status)
        {
            throw new DomainException($"A {Status.ToString().ToLowerInvariant()} order cannot change its status.");
        }

        Status = status;
        Touch(note, now, by);
    }

    private void Touch(string? note, DateTimeOffset now, string? by)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim().Length > OrderConsts.NoteMaxLength
            ? throw new DomainException($"Note is at most {OrderConsts.NoteMaxLength} characters.")
            : note.Trim();
        UpdatedAt = now;
        UpdatedBy = Author(by);
    }

    private static string? Author(string? by) => string.IsNullOrWhiteSpace(by) ? null : by.Trim()[..Math.Min(by.Trim().Length, OrderConsts.AuthorMaxLength)];
}
