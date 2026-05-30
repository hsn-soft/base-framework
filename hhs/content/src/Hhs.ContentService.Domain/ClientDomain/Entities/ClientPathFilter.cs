using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Entities;

public sealed class ClientPathFilter : Entity<Guid>
{
    public Guid ClientId { get; set; }
    [CanBeNull] public Client Client { get; set; }

    [NotNull] public string PathFilterName { get; private set; }

    public ClientFilterTypes ClientFilterType { get; internal set; }


    private ClientPathFilter()
    {
        // Not-Null string fields
        PathFilterName = string.Empty;

        // Navigation fields
        Client = null;
    }

    public ClientPathFilter(Guid clientId, [NotNull] string pathFilterName, ClientFilterTypes clientFilterType
    ) : this(Guid.CreateVersion7(), clientId, pathFilterName, clientFilterType)
    {
    }

    internal ClientPathFilter(Guid id, Guid clientId, [NotNull] string pathFilterName, ClientFilterTypes clientFilterType) : this()
    {
        Id = id;
        ClientId = clientId;

        SetPathFilterName(pathFilterName);

        ClientFilterType = clientFilterType;
    }

    internal void SetPathFilterName(string pathFilterName)
    {
        string checkFilterName = LocalizedModelValidator.NotNullOrWhiteSpace(pathFilterName, $"{nameof(ClientPathFilter)}:{nameof(PathFilterName)}", ClientPathFilterConsts.PathFilterNameMaxLength);
        PathFilterName = StringOperations.Minimize(checkFilterName);
    }
}