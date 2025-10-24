namespace HsnSoft.Base.Application.Dtos;

public interface IPagedDataRequest : ILimitedDataRequest
{
    int ResultPageNumber { get; set; }
}