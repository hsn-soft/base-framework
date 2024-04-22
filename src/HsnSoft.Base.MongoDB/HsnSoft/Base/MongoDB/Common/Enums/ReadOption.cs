namespace HsnSoft.Base.MongoDB.Common.Enums;

public enum ReadOption
{
    Primary = 0,
    PrimaryPreferred = 1,
    SecondaryPreferred = 2,
    Secondary = 3,
    Nearest = 4
}