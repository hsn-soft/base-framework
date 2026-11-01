namespace Hsx.Shared.Helper;

/// <summary>
/// The scope key of a subscription: the customer and the product, <c>{customerId:N}:{product}</c>. The identity service puts the keys of the subscriptions of a user's roles into its token (<c>allowed_scope_key</c>) and the content of a subscription carries the
/// key, so a tenant that sold the customer a product sees the data of that product and not of another product that another tenant sold the same customer. The framework only compares the texts; making them is the product's.
/// </summary>
public static class ScopeKeyHelper
{
    public static string Generate(Guid customerId, ProductTypes product)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("The customer is not given.", nameof(customerId));
        }

        return $"{customerId:N}:{product.ToCode()}";
    }
}
