namespace Abc.PublicWeb.Host.Models;

public sealed class UserNameAvailability
{
    public string UserName { get; init; } = string.Empty;

    public bool IsAvailable { get; init; }
}

public sealed class CheckModel
{
    public string UserName { get; set; } = string.Empty;

    public UserNameAvailability? Result { get; set; }

    public string? Error { get; set; }
}
