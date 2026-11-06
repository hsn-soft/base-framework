namespace Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos.Submits;

/// <summary>A client asks for a token for itself, with its secret: no user (the grant <c>client_credentials</c>). The token has the client and the audiences and nothing else, so a service lets it in only where it asks for a valid token and no permission.</summary>
public sealed class ClientTokenRequestDto : ClientRequestDto;
