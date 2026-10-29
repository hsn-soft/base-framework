using System.ComponentModel.DataAnnotations.Schema;
using HsnSoft.Base.Domain.Repositories;

namespace HsnSoft.Base.Ddd.Domain.Tests;

public sealed class EntityProjectionTests
{
    public sealed class Address
    {
        public string City { get; set; } = string.Empty;

        public string? Street { get; set; }
    }

    public sealed class Line
    {
        public string Sku { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    public sealed class Customer
    {
        public string Name { get; set; } = string.Empty;

        public Address Home { get; set; } = new();
    }

    public sealed class Order
    {
        public Guid Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public int Total { get; set; }

        public Customer Customer { get; set; } = new();

        public List<Line> Lines { get; set; } = [];

        public string Secret { get; set; } = "never";
    }

    public sealed class LineDto
    {
        public string Sku { get; set; } = string.Empty;
    }

    public sealed class OrderDto
    {
        public Guid Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public int? Total { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerHomeCity { get; set; } = string.Empty;

        public List<LineDto> Lines { get; set; } = [];

        [NotMapped]
        public string Label { get; set; } = "kept";

        public string ReadOnly { get; } = "kept";
    }

    public sealed class ArraysDto
    {
        public LineDto[] Lines { get; set; } = [];
    }

    public sealed class ReadOnlyListDto
    {
        public IReadOnlyList<LineDto> Lines { get; set; } = [];
    }

    public sealed class CustomerDto
    {
        public string Name { get; set; } = string.Empty;

        public AddressDto Home { get; set; } = new();
    }

    public sealed class AddressDto
    {
        public string City { get; set; } = string.Empty;
    }

    public sealed class OrderWithObjectDto
    {
        public CustomerDto Customer { get; set; } = new();
    }

    public sealed class UnknownDto
    {
        public string Number { get; set; } = string.Empty;

        public string Nothing { get; set; } = string.Empty;

        public string AlsoNothing { get; set; } = string.Empty;
    }

    public sealed class NoConstructorDto(string number)
    {
        public string Number { get; set; } = number;
    }

    public sealed class WrongTypeDto
    {
        public string Total { get; set; } = string.Empty;
    }

    public sealed class WrongItemDto
    {
        public List<int> Lines { get; set; } = [];
    }

    private static Order Sample() => new()
    {
        Id = Guid.NewGuid(),
        Number = "A-1",
        Total = 42,
        Customer = new Customer { Name = "Ada", Home = new Address { City = "Ankara" } },
        Lines = [new Line { Sku = "x", Quantity = 2 }, new Line { Sku = "y", Quantity = 1 }],
    };

    [Fact]
    public void SameNames_Paths_AndLists_AreFilledFromTheEntity()
    {
        Order order = Sample();

        OrderDto dto = EntityProjection.For<Order, OrderDto>().Compile()(order);

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal("A-1", dto.Number);
        Assert.Equal(42, dto.Total);
        Assert.Equal("Ada", dto.CustomerName);
        Assert.Equal("Ankara", dto.CustomerHomeCity);
        Assert.Equal(["x", "y"], dto.Lines.Select(line => line.Sku));
        Assert.Equal("kept", dto.Label);
        Assert.Equal("kept", dto.ReadOnly);
    }

    [Fact]
    public void TheSelector_IsBuiltOnce()
        => Assert.Same(EntityProjection.For<Order, OrderDto>(), EntityProjection.For<Order, OrderDto>());

    [Fact]
    public void AnArray_AnIReadOnlyList_AndAnObject_AreFilledToo()
    {
        Order order = Sample();

        Assert.Equal(["x", "y"], EntityProjection.For<Order, ArraysDto>().Compile()(order).Lines.Select(line => line.Sku));
        Assert.Equal(2, EntityProjection.For<Order, ReadOnlyListDto>().Compile()(order).Lines.Count);
        OrderWithObjectDto withObject = EntityProjection.For<Order, OrderWithObjectDto>().Compile()(order);
        Assert.Equal("Ada", withObject.Customer.Name);
        Assert.Equal("Ankara", withObject.Customer.Home.City);
    }

    [Fact]
    public void ADtoPropertyNothingAnswers_IsAnErrorThatNamesAllOfThem()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => EntityProjection.For<Order, UnknownDto>());

        Assert.Contains("UnknownDto.Nothing", error.Message);
        Assert.Contains("UnknownDto.AlsoNothing", error.Message);
        Assert.DoesNotContain("UnknownDto.Number", error.Message);
    }

    [Fact]
    public void ADtoWithoutAConstructorWithoutParameters_IsAnError()
        => Assert.Contains("constructor", Assert.Throws<InvalidOperationException>(() => EntityProjection.For<Order, NoConstructorDto>()).Message);

    [Fact]
    public void AValueThatCannotBeConverted_IsAnError()
    {
        Assert.Contains("cannot be a", Assert.Throws<InvalidOperationException>(() => EntityProjection.For<Order, WrongTypeDto>()).Message);
        Assert.Contains("cannot be a", Assert.Throws<InvalidOperationException>(() => EntityProjection.For<Order, WrongItemDto>()).Message);
    }
}
