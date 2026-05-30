using JetBrains.Annotations;

namespace Client.Test.Console;

public sealed class GetOrCreateContentRequestDto
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }

    [NotNull]
    public string DomainName { get; set; }

    [NotNull]
    public string ContentKey { get; set; }
}