using System.ComponentModel.DataAnnotations.Schema;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace HsnSoft.Base.Ddd.Domain.Tests;

/// <summary>An entity in memory becomes its DTO with the rules of the projection and no read: only when the DTO needs nothing but the entity's own properties.</summary>
public class EntityToDtoTests
{
    public sealed class Owner(string name)
    {
        public string Name { get; set; } = name;
    }

    public sealed class Line(int number)
    {
        public int Number { get; set; } = number;
    }

    public sealed class Order(Guid id, string title, int quantity) : Entity<Guid>(id)
    {
        public string Title { get; set; } = title;

        public int Quantity { get; set; } = quantity;

        public int? Discount { get; set; }

        public Owner? Owner { get; set; }

        public List<Line> Lines { get; set; } = [];
    }

    public sealed class FlatDto
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public int Quantity { get; init; }

        public int? Discount { get; init; }

        [NotMapped]
        public string Extra { get; set; } = "left alone";
    }

    public sealed class PathDto
    {
        public string Title { get; init; } = string.Empty;

        public string OwnerName { get; init; } = string.Empty;
    }

    public sealed class ListDto
    {
        public List<LineDto> Lines { get; init; } = [];
    }

    public sealed class LineDto
    {
        public int Number { get; init; }
    }

    [Fact]
    public void ADtoOfTheEntitysOwnProperties_IsFlat_AndIsMadeWithNoRead()
    {
        Order order = new(Guid.NewGuid(), "lamp", 3) { Discount = 5 };

        FlatDto dto = EntityProjection.ToDto<Order, FlatDto>(order);

        Assert.True(EntityProjection.IsFlat<Order, FlatDto>());
        Assert.Equal((order.Id, "lamp", 3, 5, "left alone"), (dto.Id, dto.Title, dto.Quantity, dto.Discount, dto.Extra));   // a [NotMapped] field is left to the service
        Assert.Null(EntityProjection.ToDto<Order, FlatDto>(new Order(Guid.NewGuid(), "x", 1)).Discount);
    }

    [Fact]
    public void ADtoThatReachesAnotherObjectOrAList_IsNotFlat_AndIsNotMadeInMemory()
    {
        Order order = new(Guid.NewGuid(), "lamp", 3) { Owner = new Owner("Ada"), Lines = [new Line(1)] };

        Assert.False(EntityProjection.IsFlat<Order, PathDto>());
        Assert.False(EntityProjection.IsFlat<Order, ListDto>());
        InvalidOperationException path = Assert.Throws<InvalidOperationException>(() => EntityProjection.ToDto<Order, PathDto>(order));
        Assert.Contains("read the DTO from the repository", path.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => EntityProjection.ToDto<Order, ListDto>(order));
    }

    [Fact]
    public void ToDto_RejectsAnEntityThatIsNotGiven()
        => Assert.Throws<ArgumentNullException>(() => EntityProjection.ToDto<Order, FlatDto>(null!));
}
