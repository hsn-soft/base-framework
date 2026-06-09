using System;

namespace HsnSoft.Base.Subscribe;

public sealed record Subscription(Guid CustomerId, Guid ProductTypeId);