using System.Diagnostics.CodeAnalysis;
using HsnSoft.Base.Domain.Entities.Auditing;

namespace Hhs.AdministrationService.Test.Shared.Models;

public class TestEntity : FullAuditedEntity<Guid>
{
    [NotNull] public string Name { get; set; }

    public int Age { get; set; }

    private TestEntity(Guid id) : base(id)
    {
        Name = string.Empty;
    }

    public TestEntity() : this(Guid.NewGuid())
    {
    }

    public TestEntity(Guid id, string name, int age) : this(id)
    {
        Name = name;
        Age = age;
    }
}