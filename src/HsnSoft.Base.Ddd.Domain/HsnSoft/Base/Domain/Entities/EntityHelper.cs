using System;

namespace HsnSoft.Base.Domain.Entities;

public static class EntityHelper
{
    public static void TrySetId<TKey>(
        IEntity<TKey> entity,
        Func<TKey> idFactory,
        bool checkForDisableIdGenerationAttribute = false)
    {
        ObjectHelper.TrySetProperty(
            entity,
            x => x.Id,
            idFactory,
            checkForDisableIdGenerationAttribute
                ? new[] { typeof(DisableIdGenerationAttribute) }
                : new Type[] { });
    }
}