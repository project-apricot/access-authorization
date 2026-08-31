namespace ApricotFramework.AccessAuthorization.Scopes;

/// <summary>
/// A scope requirement was not satisfied.
/// </summary>
public class ScopeAuthorizationException : AuthorizationException
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public ScopeAuthorizationException()
    {
        this.MissingScopes = [];
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public ScopeAuthorizationException(string? message) : base(message)
    {
        this.MissingScopes = [];
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ScopeAuthorizationException(string? message, Exception? innerException) : base(message, innerException)
    {
        this.MissingScopes = [];
    }

    /// <summary>
    /// Creates a new instance of the exception naming what was absent.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="missingScopes">The scopes that would have satisfied the requirement.</param>
    public ScopeAuthorizationException(string? message, IEnumerable<string> missingScopes) : base(message)
    {
        this.MissingScopes = [.. missingScopes];
    }

    /// <summary>
    /// Gets the scopes whose absence caused the failure.
    /// </summary>
    public IReadOnlyList<string> MissingScopes { get; }
}
