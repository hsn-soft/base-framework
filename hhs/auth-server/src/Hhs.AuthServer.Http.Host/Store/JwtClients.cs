using Hhs.Shared.Helper.Consts;

namespace Hhs.AuthServer.Store;

internal static class JwtClients
{
    public static IEnumerable<ApiScope> Scopes => new[]
    {
        new ApiScope { Name = "scope-service-administration", Resources = new List<string> { "audience-service-administration" } },
        new ApiScope { Name = "scope-service-identity", Resources = new List<string> { "audience-service-identity" } },
        new ApiScope { Name = "scope-service-tenant", Resources = new List<string> { "audience-service-tenant" } },
        new ApiScope { Name = "scope-service-content", Resources = new List<string> { "audience-service-content" } },
        new ApiScope { Name = "scope-service-text-normalizer", Resources = new List<string> { "audience-service-text-normalizer" } },
        new ApiScope { Name = "scope-service-video-generator", Resources = new List<string> { "audience-service-video-generator" } },
        new ApiScope { Name = "scope-service-event-manager", Resources = new List<string> { "audience-service-event-manager" } }   ,
        new ApiScope { Name = "scope-service-feedr-admanager", Resources = new List<string> { "audience-service-feedr-admanager" } }
    };

    public static IEnumerable<Client> Clients => new[]
    {
        new Client
        {
            ClientId = "BearerGateway_Swagger",
            ClientName = "Bearer Gateway Swagger Client",

            ClientSecrets = { new Secret("1q2w3E*_BearerGateway_Swagger_1q2w3E*".Sha256()) },
            RequireClientSecret = true,

            AllowedScopes = new List<string>
            {
                //API SCOPES (Access token)
                "scope-service-administration",
                "scope-service-identity",
                "scope-service-tenant",
                "scope-service-content",
                "scope-service-text-normalizer",
                "scope-service-video-generator",
                "scope-service-event-manager",
                "scope-service-feedr-admanager"
            },

            AccessTokenLifetime = 60 * 60 * 2, //Access token life time is 7200 seconds (2 hour)

            AllowOfflineAccess = false
        },

        new Client
        {
            ClientId = AppUiNames.CommercialClientId,
            ClientName = "Commercial Application",

            ClientSecrets = { new Secret("1q2w3E*_HhsAppCommercial_1q2w3E*".Sha256()) },
            RequireClientSecret = true,

            AllowedScopes = new List<string>
            {
                //API SCOPES (Access token)
                "scope-service-content"
            },

            AccessTokenLifetime = 60 * 60 * 2, //Access token life time is 7200 seconds (2 hour)

            AllowOfflineAccess = false
        },

        new Client
        {
            ClientId = AppUiNames.BackOfficeClientId,
            ClientName = "BackOffice Application",
            RequireClientSecret = false,

            AllowedScopes = new List<string>
            {
                //API SCOPES (Access token)
                "scope-service-administration",
                "scope-service-identity",
                "scope-service-tenant",
                "scope-service-content",
                "scope-service-text-normalizer",
                "scope-service-video-generator",
                "scope-service-event-manager",
                "scope-service-feedr-admanager",
                "offline_access"
            },

            AccessTokenLifetime = 60 * 60 * 2, //Access token life time is 7200 seconds (2 hour)

            AllowOfflineAccess = false
        },

        new Client
        {
            ClientId = "TestConsoleApp",
            ClientName = "Auth-Server Test Console App",

            // secret for authentication
            ClientSecrets = { new Secret("1q2w3E*_TestConsoleApp_1q2w3E*".Sha256()) },

            // scopes that client has access to
            AllowedScopes = new List<string>
            {
                "scope-service-administration",
                "scope-service-identity",
                "scope-service-tenant"
            }
        }
    };
}