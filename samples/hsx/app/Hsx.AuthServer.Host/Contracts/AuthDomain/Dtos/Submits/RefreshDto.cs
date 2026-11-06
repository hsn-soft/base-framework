namespace Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos.Submits;

/// <summary>A new access token for the refresh token of the cookie, through a client (the one the user signed in through or another that allows it).</summary>
public sealed class RefreshDto : ClientRequestDto;
