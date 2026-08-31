using ApricotFramework.AccessAuthorization.Examples.Web.Model;

namespace ApricotFramework.AccessAuthorization.Examples.Web.Access;

/// <summary>
/// The orders this example pretends to store, and the resource descriptions rules read.
/// </summary>
public static class OrderRepository
{
    /// <summary>
    /// The orders, by id.
    /// </summary>
    private static readonly Dictionary<string, (string Owner, bool IsPublic)> Orders = new(StringComparer.Ordinal)
    {
        ["42"] = ("reader", false),
        ["43"] = ("editor", false),
        ["44"] = ("editor", true),
    };

    /// <summary>
    /// Describes a order to the authorization pipeline.
    /// </summary>
    /// <param name="id">The order id.</param>
    /// <returns>The resource, carrying the attributes a rule may read.</returns>
    /// <remarks>
    /// Attribute values keep their domain types — <c>IsPublic</c> stays a bool rather than becoming
    /// the string "true" — so a rule pattern-matches instead of parsing.
    /// </remarks>
    public static AccessResource Describe(string id)
    {
        if (!Orders.TryGetValue(id, out var order))
        {
            return AccessResource.Create(OrderAccesses.OrderResourceType, id);
        }

        return AccessResource.Create(OrderAccesses.OrderResourceType, id, new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ownerId"] = order.Owner,
            ["isPublic"] = order.IsPublic,
        });
    }
}
