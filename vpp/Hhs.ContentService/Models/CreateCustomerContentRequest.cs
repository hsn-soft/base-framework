namespace Hhs.ContentService.Models;

public sealed record CreateCustomerContentRequest(
    string ScopeKey,
    string DomainName,
    string ContentKey);