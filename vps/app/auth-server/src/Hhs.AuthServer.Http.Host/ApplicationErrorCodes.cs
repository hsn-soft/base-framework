namespace Hhs.AuthServer;

public static class ApplicationErrorCodes
{
    public const string InvalidGrantType = "Error:AuthServer:100001";
    public const string InvalidClientCredentials = "Error:AuthServer:100002";
    public const string InvalidUserCredentials = "Error:AuthServer:100003";
    public const string InvalidScopeRequest = "Error:AuthServer:100004";
    public const string UserDisabledError = "Error:AuthServer:100005";
}