namespace HsnSoft.Base.Application.Dtos;

public interface IPagedDataRequest : ISearchDataRequest
{
    int PageNumber { get; set; }
}