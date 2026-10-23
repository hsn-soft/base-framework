using System;

namespace HsnSoft.Base;

/// <summary>The requested resource does not exist. Hosting layers answer it as "not found" (HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message)
{
}

/// <summary>A business rule rejected the request. Hosting layers answer it as "bad request" (HTTP 400).</summary>
public sealed class BusinessRuleException(string message) : Exception(message)
{
}
