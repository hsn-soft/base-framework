using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.Integration.GibInvoice.Domain.DirectoryDomain.Repositories;

public interface IExchangeUserRepository : IRepository<ExchangeUser, Guid>
{
    Task<ExchangeUser?> FindByRegisterNumberAsync(string registerNumber, CancellationToken cancellationToken = default);

    /// <summary>Replaces the whole list by the one the center gave.</summary>
    Task ReplaceAllAsync(IReadOnlyCollection<ExchangeUser> users, CancellationToken cancellationToken = default);
}
