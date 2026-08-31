namespace ApricotFramework.AccessAuthorization.AspNetCore.Impl;

/// <summary>
/// Collects rule factories in pipeline order during registration.
/// </summary>
/// <remarks>
/// Order cannot come from the service collection: a bypass rule has to precede the assigned-access
/// lookup whether it was registered before or after the call that added the lookup. Holding the
/// order here makes registration order within a stage the only thing that matters.
/// </remarks>
internal sealed class AccessRulePipeline
{
    /// <summary>
    /// Gets the factories for rules consulted before the assigned-access lookup.
    /// </summary>
    internal List<Func<IServiceProvider, IAccessRule>> BypassRules { get; } = [];

    /// <summary>
    /// Gets the factories for rules consulted after the assigned-access lookup.
    /// </summary>
    internal List<Func<IServiceProvider, IAccessRule>> Rules { get; } = [];

    /// <summary>
    /// Builds the ordered pipeline for one request.
    /// </summary>
    /// <param name="provider">The provider to resolve rules from.</param>
    /// <param name="assigned">The built-in assigned-access rule.</param>
    /// <returns>The ordered pipeline.</returns>
    internal IReadOnlyList<IAccessRule> Build(IServiceProvider provider, IAccessRule assigned)
    {
        var pipeline = new List<IAccessRule>(this.BypassRules.Count + this.Rules.Count + 1);

        pipeline.AddRange(this.BypassRules.Select(factory => factory(provider)));
        pipeline.Add(assigned);
        pipeline.AddRange(this.Rules.Select(factory => factory(provider)));

        return pipeline;
    }
}
