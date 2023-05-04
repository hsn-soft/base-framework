using System;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace HsnSoft.Base.Data;

public static class ConcurrencyStampExtensions
{
    public static void SetConcurrencyStampIfNotNull(this IHasConcurrencyStamp entity, [CanBeNull] string concurrencyStamp)
    {
        if (!concurrencyStamp.IsNullOrEmpty())
        {
            entity.ConcurrencyStamp = concurrencyStamp;
        }
    }
}
