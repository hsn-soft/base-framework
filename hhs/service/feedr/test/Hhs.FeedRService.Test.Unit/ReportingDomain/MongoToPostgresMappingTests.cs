using FluentAssertions;
using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Enums;

namespace Hhs.FeedRService.Test.Unit.ReportingDomain;

public class MongoToPostgresMappingTests
{
    [Fact]
    public void Constructor_SerializesDerivedRecordIds_AsCsv()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new[] { id1, id2 };

        var mapping = new MongoToPostgresMapping(
            Guid.NewGuid(), Guid.NewGuid(), ids, DerivationStatus.Completed);

        mapping.DerivedRecordIds.Should().Contain(id1.ToString());
        mapping.DerivedRecordIds.Should().Contain(id2.ToString());
        mapping.DerivedRecordCount.Should().Be(2);
        mapping.DerivationStatus.Should().Be(DerivationStatus.Completed);
        mapping.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyIds_SetsCountToZero()
    {
        var mapping = new MongoToPostgresMapping(
            Guid.NewGuid(), Guid.NewGuid(), Array.Empty<Guid>(), DerivationStatus.Failed, "error");

        mapping.DerivedRecordCount.Should().Be(0);
        mapping.DerivedRecordIds.Should().BeEmpty();
        mapping.ErrorMessage.Should().Be("error");
    }

    [Fact]
    public void Constructor_WithNull_HandlesGracefully()
    {
        var mapping = new MongoToPostgresMapping(
            Guid.NewGuid(), Guid.NewGuid(), null, DerivationStatus.Pending);

        mapping.DerivedRecordCount.Should().Be(0);
    }
}
