using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Domain.Services;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);
}