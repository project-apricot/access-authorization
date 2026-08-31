namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// An access requirement was not satisfied.
/// </summary>
public class AccessAuthorizationException : AuthorizationException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AccessAuthorizationException()
    {
        this.MissingAccesses = [];
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AccessAuthorizationException(string? message) : base(message)
    {
        this.MissingAccesses = [];
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AccessAuthorizationException(string? message, Exception? innerException) : base(message, innerException)
    {
        this.MissingAccesses = [];
    }

    /// <summary>
    /// Creates a new instance of the exception naming what was not granted.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="missingAccesses">The accesses that would have satisfied the requirement.</param>
    public AccessAuthorizationException(string? message, IEnumerable<string> missingAccesses) : base(message)
    {
        this.MissingAccesses = [.. missingAccesses];
    }

    /// <summary>
    /// Gets the accesses whose absence caused the failure.
    /// </summary>
    /// <remarks>
    /// For an "any" requirement this is everything asked for, since none of it was granted; for an
    /// "all" requirement it is only the part that was missing.
    /// </remarks>
    public IReadOnlyList<string> MissingAccesses { get; }
}
