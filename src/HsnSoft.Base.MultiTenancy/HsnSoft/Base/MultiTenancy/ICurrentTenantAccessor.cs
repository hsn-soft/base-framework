namespace HsnSoft.Base.MultiTenancy;

public interface ICurrentTenantAccessor
{
    BasicTenantInfo Current { get; set; }
}