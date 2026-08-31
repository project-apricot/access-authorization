using ApricotFramework.AccessAuthorization.Examples.Web.Model;

namespace ApricotFramework.AccessAuthorization.Examples.Web.Access;

/// <summary>
/// Decides from the order itself: its owner may edit it, and anyone may read a public one.
/// </summary>
/// <remarks>
/// The resource-aware half of the model, and what an assigned access cannot express. Registered after
/// the assigned lookup, so it widens rather than replaces. Note that it reads a string and a bool
/// without parsing either.
/// </remarks>
public sealed class OrderAttributeRule : AccessRule
{
    /// <inheritdoc />
    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        if (context.Resource is null || !string.Equals(context.Resource.Type, OrderAccesses.OrderResourceType, StringComparison.Ordinal))
        {
            return Task.FromResult(AccessDecision.Abstain);
        }

        var attributes = context.Resource.Attributes;

        if (string.Equals(access, OrderAccesses.Edit, StringComparison.Ordinal)
            && attributes.GetValueOrDefault("ownerId") is string owner
            && string.Equals(owner, context.Subject.Id, StringComparison.Ordinal))
        {
            return Task.FromResult(AccessDecision.Allow);
        }

        if (string.Equals(access, OrderAccesses.Read, StringComparison.Ordinal) && attributes.GetValueOrDefault("isPublic") is true)
        {
            return Task.FromResult(AccessDecision.Allow);
        }

        return Task.FromResult(AccessDecision.Abstain);
    }
}
