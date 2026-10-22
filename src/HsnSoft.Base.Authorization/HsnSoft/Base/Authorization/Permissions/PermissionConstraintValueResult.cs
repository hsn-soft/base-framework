namespace HsnSoft.Base.Authorization.Permissions;

public sealed class PermissionConstraintValueResult
{
    public bool HasValue { get; }

    public string Value { get; }

    private PermissionConstraintValueResult(bool hasValue, string value)
    {
        HasValue = hasValue;
        Value = value;
    }

    public static PermissionConstraintValueResult NotFound => new(false, null);

    public static PermissionConstraintValueResult Found(string value) => new(true, value);
}