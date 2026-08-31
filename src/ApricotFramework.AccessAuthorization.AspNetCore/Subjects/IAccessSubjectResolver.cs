using System.Security.Claims;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Subjects;

/// <summary>
/// Turns an authenticated principal into the subject a decision is made about.
/// </summary>
public interface IAccessSubjectResolver
{
    /// <summary>
    /// Resolves the subject for a principal.
    /// </summary>
    /// <param name="principal">The principal, which may be unauthenticated or absent.</param>
    /// <returns>
    /// The subject, or <see langword="null"/> when the principal carries none — an anonymous caller,
    /// or a machine token issued without a subject.
    /// </returns>
    AccessSubject? Resolve(ClaimsPrincipal? principal);
}
