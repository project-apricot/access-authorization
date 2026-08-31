namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// One access the application knows about.
/// </summary>
/// <param name="Access">The access string, such as <c>sales:orders:read</c>.</param>
/// <param name="ResourceType">
/// The resource type the access applies to, or <see langword="null"/> when it applies regardless.
/// Used to keep an enumeration for one resource from probing unrelated accesses.
/// </param>
public sealed record AccessDefinition(string Access, string? ResourceType = null);
