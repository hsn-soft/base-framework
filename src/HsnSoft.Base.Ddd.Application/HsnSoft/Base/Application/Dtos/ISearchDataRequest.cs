namespace HsnSoft.Base.Application.Dtos;

public interface ISearchDataRequest : ISortedDataRequest
{
    string SearchText { get; set; }
}