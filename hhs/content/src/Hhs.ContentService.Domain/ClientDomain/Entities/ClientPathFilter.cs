using System.Globalization;
using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Entities;

public sealed class ClientPathFilter : Entity<Guid>
{
    public Guid ClientId { get; private set; }

    [NotNull]
    public string PathFilterName { get; private set; }

    public ClientFilterTypes ClientFilterType { get; internal set; }

    private ClientPathFilter()
    {
        PathFilterName = string.Empty;
    }

    internal ClientPathFilter(Guid id, Guid clientId, [NotNull] string pathFilterName, ClientFilterTypes clientFilterType) : this()
    {
        Id = id;
        SetClientId(clientId);
        SetPathFilterName(pathFilterName);

        ClientFilterType = clientFilterType;
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
    }

    internal void SetPathFilterName(string pathFilterName)
    {
        PathFilterName = Check.NotNull(pathFilterName, nameof(pathFilterName), ClientPathFilterConsts.PathFilterNameMaxLength).ToLower(new CultureInfo("en-US"));
    }
}