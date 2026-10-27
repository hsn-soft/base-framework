using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Domain.Entities;

namespace HsnSoft.Base.Ddd.Application.Tests;

public class AuditedDtoContractsTests
{
    private sealed class CreatedDto : CreationAuditedEntityDto<Guid>;

    private sealed class ChangedDto : AuditedEntityDto<Guid>;

    private sealed class FullDto : FullAuditedEntityDto<Guid>;

    private sealed class Row() : FullAuditedEntity<Guid>(Guid.NewGuid());

    [Fact]
    public void ADto_HasTheAuditContractOfItsLevel_TheSameOnesAsTheEntity()
    {
        Assert.IsType<ICreationAuditedObject>(new CreatedDto(), exactMatch: false);
        Assert.IsType<IAuditedObject>(new ChangedDto(), exactMatch: false);
        Assert.IsType<IFullAuditedObject>(new FullDto(), exactMatch: false);
        Assert.IsType<IFullAuditedObject>(new Row(), exactMatch: false);
    }

    [Fact]
    public void OneRule_ReadsTheAuditFieldsOfAnEntityAndOfItsDto()
    {
        Guid user = Guid.NewGuid();
        DateTimeOffset at = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
        Row row = new();
        row.MarkCreated(at, user);
        FullDto dto = new() { CreatedAt = row.CreatedAt, CreatedBy = row.CreatedBy };

        static (DateTimeOffset, Guid?) Created(ICreationAuditedObject audited) => (audited.CreatedAt, audited.CreatedBy);

        Assert.Equal(Created(row), Created(dto));
        Assert.Equal((at, user), Created(dto));
    }
}
