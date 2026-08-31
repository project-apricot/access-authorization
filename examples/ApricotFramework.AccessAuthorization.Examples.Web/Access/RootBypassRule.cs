namespace ApricotFramework.AccessAuthorization.Examples.Web.Access;

/// <summary>
/// Grants an operator everything, without the assigned-access lookup being consulted at all.
/// </summary>
/// <remarks>
/// Registered as a bypass rule, so it runs before the lookup. This is the shape the predecessor
/// hard-coded as a claim check inside the library.
/// </remarks>
public sealed class RootBypassRule : AccessRule
{
    /// <inheritdoc />
    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(string.Equals(context.Subject.Id, "root", StringComparison.Ordinal)
            ? AccessDecision.Allow
            : AccessDecision.Abstain);
    }
}
