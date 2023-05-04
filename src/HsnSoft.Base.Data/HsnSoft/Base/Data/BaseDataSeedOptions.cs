namespace HsnSoft.Base.Data;

public class BaseDataSeedOptions
{
    public DataSeedContributorList Contributors { get; }

    public BaseDataSeedOptions()
    {
        Contributors = new DataSeedContributorList();
    }
}