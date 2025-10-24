namespace HsnSoft.Base.Application.Dtos;

public interface ILimitedDataRequest
{
    int MaxResultCount { get; set; }
}