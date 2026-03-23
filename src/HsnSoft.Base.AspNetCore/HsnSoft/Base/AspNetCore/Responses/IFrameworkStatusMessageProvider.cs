namespace HsnSoft.Base.AspNetCore.Responses;

public interface IFrameworkStatusMessageProvider
{
    string GetMessage(int statusCode);
}