namespace HsnSoft.Base.MongoDB.Options;

public class FilterOptions
{
    /// <summary>
    /// Min 1, Don't set if not used. Limit = PageSize
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Start from 1, Don't set if not used. Skip = (Page - 1) * PageSize
    /// </summary>
    public int? Page { get; set; }

    public ReadOption ReadOption { get; set; } = ReadOption.Primary;
}