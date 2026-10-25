namespace Abc.IdentityService.Domain.UserDomain.Consts;

public static class UserConsts
{
    public const string TableName = "users";

    public const int UserNameMaxLength = 64;
    public const int EmailMaxLength = 256;
    public const int DisplayNameMaxLength = 128;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int PasswordHashMaxLength = 512;
    public const int RoleMaxLength = 32;
}
