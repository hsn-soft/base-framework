using JetBrains.Annotations;

namespace HsnSoft.Base.Test.Api.Application.Contracts;

public class SearchFilterDto
{
    [CanBeNull] public string SearchText { get; set; }
}

public class GetUserFilterDto
{
    [CanBeNull] public string FirstName { get; set; }
    [CanBeNull] public string LastName { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}

public class SearchOrderedFilterDto : SearchFilterDto
{
    [CanBeNull] public string OrderByText { get; set; }
}

public class GetOrderedUserFilterDto : GetUserFilterDto
{
    [CanBeNull] public string OrderByText { get; set; }
}

public class GetPagingUserFilterDto : GetOrderedUserFilterDto
{
    public uint? PageNumber { get; set; }
    public uint? PageSize { get; set; }
}