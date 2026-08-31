namespace ApricotFramework.AccessAuthorization.Examples.Web.Access;

/// <summary>
/// A typed view over an <see cref="AccessSubject"/> for the part of the application that only deals
/// with users acting inside an organisation.
/// </summary>
/// <remarks>
/// The recommended shape for application-specific identity. <see cref="AccessSubject"/> is sealed
/// because it computes the cache key from its id and attributes alone — a subclass adding fields
/// would leave them out of that key, so two different identities would share one cache entry. A
/// projection sidesteps that: the canonical subject stays the thing that identifies and keys, and
/// this carries the typed reading of it.
/// </remarks>
public sealed class OrganizationSubject
{
    /// <summary>
    /// The subject attribute the organisation is carried in.
    /// </summary>
    public const string OrganizationAttribute = "organization";

    /// <summary>
    /// Creates a new typed view.
    /// </summary>
    /// <param name="subject">The subject being viewed.</param>
    /// <param name="organizationId">The organisation the subject is acting in.</param>
    private OrganizationSubject(AccessSubject subject, string organizationId)
    {
        this.Subject = subject;
        this.OrganizationId = organizationId;
    }

    /// <summary>
    /// Gets the subject this view was projected from, which is what the pipeline keys on.
    /// </summary>
    public AccessSubject Subject { get; }

    /// <summary>
    /// Gets the user id.
    /// </summary>
    public string UserId => this.Subject.Id;

    /// <summary>
    /// Gets the organisation the subject is acting in. Never null.
    /// </summary>
    public string OrganizationId { get; }

    /// <summary>
    /// Projects a subject that is acting inside an organisation.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="projected">The typed view, when the subject carries an organisation.</param>
    /// <returns><see langword="true"/> when the subject carries an organisation.</returns>
    /// <remarks>
    /// Validating once here is the point: a rule or store then works with a non-null
    /// <see cref="OrganizationId"/> instead of repeating a keyed lookup and a null check.
    /// </remarks>
    public static bool TryFrom(AccessSubject subject, out OrganizationSubject? projected)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var organization = subject.Attributes.GetValueOrDefault(OrganizationAttribute);

        projected = string.IsNullOrWhiteSpace(organization) ? null : new OrganizationSubject(subject, organization);

        return projected is not null;
    }
}
