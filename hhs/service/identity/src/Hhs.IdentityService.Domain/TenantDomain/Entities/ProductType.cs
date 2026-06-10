using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;


public sealed class ProductType : Entity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string Code { get; private set; }

    [NotNull] public string NormalizedCode { get; private set; }

    [NotNull] public string Name { get; private set; }

    [NotNull] public string NormalizedName { get; private set; }

    private ProductType()
    {
        // Not-Null string fields
        Code = string.Empty;
        NormalizedCode = string.Empty;
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    internal ProductType(
        [NotNull]  string code,
        [NotNull]  string name
    ) : this(Guid.CreateVersion7(),  code: code, name: name)
    {
    }

    internal ProductType(Guid id,
        [NotNull]  string code,
        [NotNull]  string name
    ) : this()
    {
        Id = id;

        SetCode(code);
        SetName(name);
    }

    internal void SetCode(string code)
    {
        Code = LocalizedModelValidator.NotNullOrWhiteSpace(code, $"{nameof(Tenant)}:{nameof(Name)}", ProductTypeConsts.CodeMaxLength);
        NormalizedCode = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(code));
    }

    internal void SetName(string name)
    {
        Name = LocalizedModelValidator.NotNullOrWhiteSpace(name, $"{nameof(Tenant)}:{nameof(Name)}", ProductTypeConsts.NameMaxLength);
        NormalizedName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(name));
    }
}