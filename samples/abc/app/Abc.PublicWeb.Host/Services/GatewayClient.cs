using Abc.PublicWeb.Host.Models;

namespace Abc.PublicWeb.Host.Services;

/// <summary>The calls of the public web application to the public gateway: <c>/identity-service/v1/...</c>, mapped to the <c>public</c> endpoints of the service.</summary>
public sealed class GatewayClient(HttpClient http)
{
    public async Task<UserNameAvailability?> CheckUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync("/identity-service/v1/users/check-user-name", new { userName }, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        ApiResponse<UserNameAvailability>? body = await response.Content.ReadFromJsonAsync<ApiResponse<UserNameAvailability>>(cancellationToken).ConfigureAwait(false);
        return body?.Payload;
    }
}
