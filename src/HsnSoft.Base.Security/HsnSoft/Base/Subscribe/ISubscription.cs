using System;

namespace HsnSoft.Base.Subscribe;

public interface ISubscription
{
    Guid CustomerId { get; }
    Guid ProductTypeId { get; set; }
}