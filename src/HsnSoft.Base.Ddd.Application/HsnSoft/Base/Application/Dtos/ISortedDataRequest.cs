namespace HsnSoft.Base.Application.Dtos;

public interface ISortedDataRequest : ILimitedDataRequest
{
    string SortingText { get; set; }
}