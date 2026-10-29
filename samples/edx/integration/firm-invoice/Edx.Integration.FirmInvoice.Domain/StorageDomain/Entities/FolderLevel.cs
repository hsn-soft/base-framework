namespace Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;

/// <summary>What a folder of the storage stands for. The hierarchy is always <see cref="Tenant"/>, <see cref="Year"/>, <see cref="MonthDay"/>, <see cref="HourMinute"/>, from the top.</summary>
public enum FolderLevel
{
    Tenant = 0,
    Year = 1,
    MonthDay = 2,
    HourMinute = 3,
}
