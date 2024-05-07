namespace HsnSoft.Base.MongoDB.Options;

public enum ReadOption
{
    Primary = 0,
    PrimaryPreferred = 1,
    SecondaryPreferred = 2,
    Secondary = 3,
    Nearest = 4
}