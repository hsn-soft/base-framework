using HsnSoft.Base.Timing;

namespace HsnSoft.Base.MongoDB;

public class BaseMongoDbOptions
{
    /// <summary>
    /// Serializer the datetime based on <see cref="BaseClockOptions.Kind"/> in MongoDb.
    /// Default: true.
    /// </summary>
    public bool UseBaseClockHandleDateTime { get; set; }

    public BaseMongoDbOptions()
    {
        UseBaseClockHandleDateTime = true;
    }
}