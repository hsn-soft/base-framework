using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;

namespace Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;

/// <summary>
/// A user of the e-invoice system, as the center's list of the registered users says it (<c>newUserPkListxml_full</c>): a taxpayer with the aliases of its post boxes. The service keeps a copy, refreshed from the center (the guides say: at most once an hour), because an invoice has to be
/// addressed to the post box of its customer. The list is the center's, not a tenant's: it is not filtered by tenant.
/// </summary>
public sealed class ExchangeUser : Entity<Guid>
{
    // for the database driver
    private ExchangeUser()
    {
    }

    public string RegisterNumber { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>The aliases of its post boxes that are in force (the newest first).</summary>
    public IReadOnlyList<string> PostBoxAliases { get; private set; } = [];

    public DateTimeOffset RefreshedAt { get; private set; }

    public static ExchangeUser Of(string registerNumber, string title, IEnumerable<string> postBoxAliases, DateTimeOffset at)
        => new()
        {
            Id = Guid.CreateVersion7(),
            RegisterNumber = Guard.NotNullOrWhiteSpace(registerNumber, nameof(RegisterNumber), 32),
            Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), 300),
            PostBoxAliases = [.. postBoxAliases],
            RefreshedAt = at,
        };

    /// <summary>The list was read again: the title and the aliases are the ones of the new list.</summary>
    public void Refresh(string title, IEnumerable<string> postBoxAliases, DateTimeOffset at)
    {
        Title = Guard.NotNullOrWhiteSpace(title, nameof(Title), 300);
        PostBoxAliases = [.. postBoxAliases];
        RefreshedAt = at;
    }
}
