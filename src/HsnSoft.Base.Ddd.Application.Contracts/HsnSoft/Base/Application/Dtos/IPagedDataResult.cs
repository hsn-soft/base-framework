namespace HsnSoft.Base.Application.Dtos;

public interface IPagedDataResult<T> : IListDataResult<T>, IHasTotalCount, IPagedDataRequest;