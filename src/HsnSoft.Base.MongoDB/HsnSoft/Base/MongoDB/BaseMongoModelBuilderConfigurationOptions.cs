using JetBrains.Annotations;

namespace HsnSoft.Base.MongoDB;

public class BaseMongoModelBuilderConfigurationOptions
{
    private string _collectionPrefix;

    public BaseMongoModelBuilderConfigurationOptions([NotNull] string collectionPrefix = "")
    {
        Check.NotNull(collectionPrefix, nameof(collectionPrefix));

        CollectionPrefix = collectionPrefix;
    }

    [NotNull]
    public string CollectionPrefix
    {
        get => _collectionPrefix;
        set
        {
            Check.NotNull(value, nameof(value), $"{nameof(CollectionPrefix)} can not be null! Set to empty string if you don't want a collection prefix.");
            _collectionPrefix = value;
        }
    }
}