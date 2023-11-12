using System;
using JetBrains.Annotations;

namespace HsnSoft.Base.MultiTenancy;

public interface ICurrentTenant
{
    bool IsAvailable { get; }

    [CanBeNull]
    Guid? Id { get; }

    [CanBeNull]
    string Name { get; }

    [CanBeNull]
    string Domain { get; }

    IDisposable Change(Guid? id, string name = null, string domain = null);
}
