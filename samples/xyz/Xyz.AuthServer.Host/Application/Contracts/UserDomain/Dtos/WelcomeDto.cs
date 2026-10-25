namespace Xyz.AuthServer.Application.Contracts.UserDomain.Dtos;

public sealed class WelcomeDto
{
    /// <summary>The greeting, in the language of the request.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The language it is told in (<c>en</c>, <c>tr</c>, <c>ru</c>).</summary>
    public string Language { get; init; } = string.Empty;
}
