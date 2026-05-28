using System.Globalization;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Test.Api.Domain.Consts;
using HsnSoft.Base.Test.Api.Domain.Enums;
using JetBrains.Annotations;

namespace HsnSoft.Base.Test.Api.Domain.Entities;

public class User : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }
    [NotNull] public string Email { get; private set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }

    public UserOperationStates Status { get; set; }

    private User(Guid id) : base(id)
    {
        Email = string.Empty;
        Status = UserOperationStates.None;
    }

    public User() : this(Guid.CreateVersion7())
    {
    }

    public User(Guid id, Guid tenantId, [NotNull] string email, [CanBeNull] string firstName = null, [CanBeNull] string lastName = null, int age = 0) : this(id)
    {
        TenantId = tenantId;
        SetEmail(email);
        FirstName = firstName;
        LastName = lastName;
        Age = age;
    }

    public void SetEmail(string email)
    {
        Email = Check.NotNull(email, nameof(email), UserConsts.EmailMaxLength).ToLower(new CultureInfo("en-US"));
    }
}