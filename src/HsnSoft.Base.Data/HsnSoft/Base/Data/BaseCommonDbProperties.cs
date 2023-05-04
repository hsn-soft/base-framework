namespace HsnSoft.Base.Data;

public static class BaseCommonDbProperties
{
    /// <summary>
    /// This table prefix is shared by most of the Base modules.
    /// You can change it to set table prefix for all modules using this.
    /// 
    /// Default value: "Base".
    /// </summary>
    public static string DbTablePrefix { get; set; } = "Base";

    /// <summary>
    /// Default value: null.
    /// </summary>
    public static string DbSchema { get; set; } = null;
}