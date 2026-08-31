namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Base class for the authorization failures this library raises, so a host can catch both halves
/// with one clause.
/// </summary>
public class AuthorizationException : Exception
{
    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    public AuthorizationException()
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    public AuthorizationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AuthorizationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
