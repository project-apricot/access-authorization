namespace ApricotFramework.AccessAuthorization.Examples.Web.Model;

/// <summary>
/// The accesses this example declares. A real service usually already has a class like this.
/// </summary>
public static class OrderAccesses
{
    /// <summary>
    /// The resource type order accesses apply to.
    /// </summary>
    public const string OrderResourceType = "sales:order";

    /// <summary>
    /// Read one order.
    /// </summary>
    public const string Read = "sales:orders:read";

    /// <summary>
    /// Edit one order.
    /// </summary>
    public const string Edit = "sales:orders:edit";

    /// <summary>
    /// Delete one order.
    /// </summary>
    public const string Delete = "sales:orders:delete";

    /// <summary>
    /// Gets the catalog handed to the library, which is what makes a capability listing possible.
    /// </summary>
    /// <returns>The declared accesses.</returns>
    public static IReadOnlyList<AccessDefinition> All()
    {
        return
        [
            new AccessDefinition(Read, OrderResourceType),
            new AccessDefinition(Edit, OrderResourceType),
            new AccessDefinition(Delete, OrderResourceType),
        ];
    }
}
