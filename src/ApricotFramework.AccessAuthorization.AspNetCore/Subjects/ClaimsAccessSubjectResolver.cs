using System.Security.Claims;
using ApricotFramework.AccessAuthorization.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Subjects;

/// <summary>
/// Reads the subject id and its attributes from the principal's claims.
/// </summary>
public class ClaimsAccessSubjectResolver : IAccessSubjectResolver
{
    /// <summary>
    /// The settings, read per call, so a configuration reload takes effect without a restart.
    /// </summary>
    private readonly IOptionsMonitor<AccessAuthorizationOptions> options;

    /// <summary>
    /// Creates a new resolver.
    /// </summary>
    /// <param name="options">The settings.</param>
    public ClaimsAccessSubjectResolver(IOptionsMonitor<AccessAuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    /// <inheritdoc />
    public virtual AccessSubject? Resolve(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var settings = this.GetCurrentOptions().Subject;
        var id = FindId(principal, settings);

        // No id means no subject, and the caller denies. The predecessor passed the null through to
        // the data store instead.
        return id is null ? null : AccessSubject.Create(id, ReadAttributes(principal, settings));
    }

    /// <summary>
    /// Gets the settings in force for this call.
    /// </summary>
    /// <returns>The settings.</returns>
    protected virtual AccessAuthorizationOptions GetCurrentOptions()
    {
        return this.options.CurrentValue;
    }

    /// <summary>
    /// Finds the first configured claim carrying a subject id.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="settings">The subject settings.</param>
    /// <returns>The subject id, or <see langword="null"/> when no claim carries one.</returns>
    private static string? FindId(ClaimsPrincipal principal, AccessSubjectOptions settings)
    {
        foreach (var claimType in settings.IdClaimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Lifts the configured claims onto the subject as attributes.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="settings">The subject settings.</param>
    /// <returns>The attributes, or <see langword="null"/> when none apply.</returns>
    private static Dictionary<string, string>? ReadAttributes(ClaimsPrincipal principal, AccessSubjectOptions settings)
    {
        Dictionary<string, string>? attributes = null;

        foreach (var mapping in settings.AttributeClaims)
        {
            var value = principal.FindFirst(mapping.Key)?.Value;

            if (value is null)
            {
                continue;
            }

            attributes ??= new Dictionary<string, string>(StringComparer.Ordinal);
            attributes[mapping.Value] = value;
        }

        return attributes;
    }
}
