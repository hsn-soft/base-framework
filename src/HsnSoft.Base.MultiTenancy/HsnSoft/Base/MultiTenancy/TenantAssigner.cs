using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>
/// Gives a new <see cref="IMultiTenant"/> entity the current tenant, for the persistence that cannot do it through a change tracker (MongoDB): it writes the <c>TenantId</c> property, whatever the access of its setter.
/// The entity keeps the property with a private setter and has no method for it.
/// </summary>
public static class TenantAssigner
{
    private static readonly ConcurrentDictionary<Type, MethodInfo?> s_setters = new();

    /// <summary>Sets the tenant of <paramref name="entity"/> when it has none (<see cref="Guid.Empty"/>); an entity that already has one keeps it.</summary>
    /// <exception cref="InvalidOperationException">The entity has no <c>TenantId</c> property that can be set.</exception>
    public static void Assign(IMultiTenant entity, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.TenantId != Guid.Empty)
        {
            return;
        }

        MethodInfo setter = s_setters.GetOrAdd(entity.GetType(), type => type.GetProperty(nameof(IMultiTenant.TenantId))?.GetSetMethod(nonPublic: true))
                            ?? throw new InvalidOperationException($"{entity.GetType().Name} has no TenantId property with a setter (it may be private).");
        setter.Invoke(entity, [tenantId]);
    }
}
