using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class Company : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    [NotNull]  public string Title { get; private set; }

    [NotNull] public string Name { get; private set; }

    [NotNull] public string NormalizedName { get; private set; }

    public ICollection<Customer> Customers { get; set; }

    private Company()
    {
        // Not-Null string fields
        Title = string.Empty;
        Name = string.Empty;
        NormalizedName = string.Empty;

        // navigation fields
        Customers = [];
    }

    internal Company(
        [NotNull] string title,
        [NotNull] string name
    ) : this(Guid.CreateVersion7(),  title: title, name: name)
    {
    }

    internal Company(Guid id,
        [NotNull] string title,
        [NotNull] string name
    ) : this()
    {
        Id = id;

        SetTitle(title);
        SetName(name);
    }

    internal void SetTitle(string title)
        => Title = LocalizedModelValidator.NotNullOrWhiteSpace(title, $"{nameof(Tenant)}:{nameof(Title)}", CompanyConsts.TitleMaxLength);

    internal void SetName(string name)
    {
        Name = LocalizedModelValidator.NotNullOrWhiteSpace(name, $"{nameof(Tenant)}:{nameof(Name)}", CompanyConsts.NameMaxLength);
        NormalizedName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(name));
    }
}