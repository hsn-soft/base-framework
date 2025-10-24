namespace HsnSoft.Base.Application.Dtos;

public interface IPagedDataResult<T> : IListDataResult<T>, IHasTotalCount
{
    int PageTotalCount { get;  }
    int ResultDataStartNumber { get;  }
    int ResultDataEndNumber { get; }
}