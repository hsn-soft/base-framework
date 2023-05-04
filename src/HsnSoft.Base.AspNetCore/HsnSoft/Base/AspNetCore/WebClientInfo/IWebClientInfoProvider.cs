namespace HsnSoft.Base.AspNetCore.WebClientInfo;

public interface IWebClientInfoProvider
{
    string BrowserInfo { get; }

    string ClientIpAddress { get; }
}