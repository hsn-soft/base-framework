using Abc.ProductService.Domain.ProductDomain.Entities;
using Abc.ProductService.Domain.ProductStockHistoryDomain.Consts;
using Abc.ProductService.Domain.ProductStockHistoryDomain.Entities;
using Abc.Shared.Contracts.Events.Order;
using HsnSoft.Base.Domain.Repositories;

namespace Abc.ProductService.Application.Services;

/// <summary>The answer of a stock check: the stock of every line was set aside, or nothing was and why.</summary>
public sealed record StockReservation(bool IsReserved, string? Reason, decimal TotalAmount = 0);

/// <summary>
/// Sets the stock of an order aside: every line is checked first (the product exists, is for sale and has enough), and only when all can be given is any taken, each with a record in the stock history. A line that an earlier attempt
/// took already (its record is there) is not taken again, so a message that comes twice does no harm. There is no transaction over the products and their history (two databases): a failure in the middle is finished by the next attempt.
/// </summary>
public sealed class StockReservationService(IRepository<Product, Guid> products, IRepository<ProductStockHistory, Guid> stockHistory)
{
    public async Task<StockReservation> ReserveAsync(Guid orderId, IReadOnlyList<OrderLineEto> lines, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);

        List<(Product Product, int Quantity, string Reference)> toTake = [];
        List<string> problems = [];
        decimal total = 0;
        foreach (IGrouping<Guid, OrderLineEto> line in lines.GroupBy(line => line.ProductId))
        {
            int quantity = line.Sum(item => item.Quantity);
            string reference = $"order:{orderId}:{line.Key}";
            Product? product = await products.FindAsync(line.Key, cancellationToken).ConfigureAwait(false);
            if (product is not null)
            {
                total += product.Price * quantity;
            }

            if (await stockHistory.AnyAsync(history => history.Reference == reference && history.Reason == ProductStockHistoryConsts.Reserved, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (product is null)
            {
                problems.Add($"Product {line.Key} was not found.");
            }
            else if (!product.IsActive)
            {
                problems.Add($"{product.Name} is not for sale.");
            }
            else if (product.Stock < quantity)
            {
                problems.Add($"{product.Name}: {product.Stock} in stock, {quantity} asked for.");
            }
            else
            {
                toTake.Add((product, quantity, reference));
            }
        }

        if (problems.Count > 0)
        {
            return new StockReservation(false, string.Join(" ", problems));
        }

        foreach ((Product product, int quantity, string reference) in toTake)
        {
            int before = product.Stock;
            product.Reserve(quantity);
            await products.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await stockHistory.AddAsync(ProductStockHistory.Record(product.Id, product.Name, before, product.Stock, ProductStockHistoryConsts.Reserved, $"order {orderId}", reference), cancellationToken).ConfigureAwait(false);
        }

        return new StockReservation(true, null, total);
    }
}
