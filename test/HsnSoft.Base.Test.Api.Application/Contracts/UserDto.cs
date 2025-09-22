namespace HsnSoft.Base.Test.Api.Application.Contracts;

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
}

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
}

public class CreateUserDto
{
    public string FullName { get; set; } = "";
}

public class UpdateUserDto
{
    public string FullName { get; set; } = "";
}

public class DeleteUserDto
{
    public string FullName { get; set; } = "";
}