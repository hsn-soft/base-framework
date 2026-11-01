namespace HsnSoft.Base.MultiTenancy;

/// <summary>
/// An entity that belongs to a <b>scope</b>, not to a tenant: the data of one subscription (a customer's product, say) that several tenants may manage, each the part it sold. The scope key is a text the product makes (hhs: the customer and the product,
/// <c>{customerId:N}:{product}</c>); the persistence shows a caller only the entities whose key its token lists (<c>allowed_scope_key</c>, <see cref="ICurrentTenant.AllowedScopeKeys"/>), and the system tenant sees every key.
/// <para>It is another isolation than <see cref="IMultiTenant"/>, and an entity has one of them, never both: a tenant owns its users and its roles (<see cref="IMultiTenant"/>) while the content of a subscription (<see cref="IScopeSubscription"/>) belongs to the subscription whichever tenant sold it.
/// Both kinds of entities may be in one context, in different tables. The product sets <see cref="ScopeKey"/> itself: only it knows which subscription a new row belongs to (the persistence does not fill it in).</para>
/// </summary>
public interface IScopeSubscription
{
    string ScopeKey { get; }
}
