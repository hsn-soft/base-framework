namespace Xyz.Shared.Hosting;

/// <summary>The requested resource does not exist. Becomes HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>A business rule rejected the request. Becomes HTTP 400.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
