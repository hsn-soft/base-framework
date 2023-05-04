using System.Collections.Generic;

namespace HsnSoft.Base.Domain.Entities;

public interface IGeneratesDomainEvents
{
    IEnumerable<DomainEventRecord> GetDomainEvents();

    void ClearDomainEvents();
}