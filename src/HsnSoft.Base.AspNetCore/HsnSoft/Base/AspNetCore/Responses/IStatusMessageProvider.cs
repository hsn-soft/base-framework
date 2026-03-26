namespace HsnSoft.Base.AspNetCore.Responses;

public interface IStatusMessageProvider
{
    string GetMessage(int statusCode);
}