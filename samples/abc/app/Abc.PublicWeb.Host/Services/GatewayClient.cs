using Abc.PublicWeb.Host.Models;
using HsnSoft.Base;
using HsnSoft.Base.Http;

namespace Abc.PublicWeb.Host.Services;

/// <summary>The calls of the public web application to the public gateway: <c>/identity-service/v1/...</c>, mapped to the <c>public</c> endpoints of the service.</summary>
public sealed class GatewayClient(HttpClient http)
{
    public async Task<UserNameAvailability?> CheckUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        try
        {
            return await http.PostPayloadAsync<UserNameAvailability>("/identity-service/v1/users/check-user-name", new { userName }, cancellationToken).ConfigureAwait(false);
        }
        catch (BaseHttpException exception) when (exception.HttpStatusCode < 500)
        {
            return null;
        }
    }
}
