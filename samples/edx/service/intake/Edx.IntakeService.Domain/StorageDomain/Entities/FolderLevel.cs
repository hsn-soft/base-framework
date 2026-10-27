namespace Edx.IntakeService.Domain.StorageDomain.Entities;

/// <summary>What a folder of the storage stands for. The hierarchy is always <see cref="Tenant"/>, <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, <see cref="Hour"/>, <see cref="Minute"/>, from the top.</summary>
public enum FolderLevel
{
    Tenant = 0,
    Year = 1,
    Month = 2,
    Day = 3,
    Hour = 4,
    Minute = 5,
}
