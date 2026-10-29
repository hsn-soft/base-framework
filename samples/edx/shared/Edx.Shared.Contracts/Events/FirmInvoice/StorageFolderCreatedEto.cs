using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.FirmInvoice;

/// <summary>A folder of the storage hierarchy was made (its record was written). Published by the firm invoice integration for every folder record, so whoever keeps a view of the hierarchy can follow it.</summary>
/// <param name="FolderId">The id of the record.</param>
/// <param name="ParentId">The folder above it; none for the folder of a tenant.</param>
/// <param name="Name">The name of the folder (a tenant id, a year, a month, ...).</param>
/// <param name="Path">The path from the tenant folder down, with <c>/</c>.</param>
/// <param name="Level">What the folder stands for: <c>Tenant</c>, <c>Year</c>, <c>Month</c>, <c>Day</c>, <c>Hour</c> or <c>Minute</c>.</param>
public sealed record StorageFolderCreatedEto(Guid FolderId, Guid? ParentId, string Name, string Path, string Level) : IIntegrationEventMessage;
