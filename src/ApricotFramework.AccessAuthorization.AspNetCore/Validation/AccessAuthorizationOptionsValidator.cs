using ApricotFramework.AccessAuthorization.Options;
using ApricotFramework.AccessAuthorization.Scopes.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Validation;

/// <summary>
/// Rejects settings that would make every request fail for a reason nobody can see.
/// </summary>
public sealed class AccessAuthorizationOptionsValidator : IValidateOptions<AccessAuthorizationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AccessAuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.CacheLifetime is { } lifetime && lifetime < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.CacheLifetime)} cannot be negative. Leave it unset to disable caching.");
        }

        if (options.Subject.IdClaimTypes.Count == 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.Subject)}.{nameof(AccessSubjectOptions.IdClaimTypes)} cannot be empty; no subject could ever be resolved.");
        }

        if (options.Subject.IdClaimTypes.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.Subject)}.{nameof(AccessSubjectOptions.IdClaimTypes)} cannot contain a blank claim type.");
        }

        var blankAttribute = options.Subject.AttributeClaims.FirstOrDefault(mapping => string.IsNullOrWhiteSpace(mapping.Key) || string.IsNullOrWhiteSpace(mapping.Value));

        if (blankAttribute.Key is not null || blankAttribute.Value is not null)
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.Subject)}.{nameof(AccessSubjectOptions.AttributeClaims)} cannot map a blank claim type or attribute name.");
        }

        if (options.Scope.ClaimTypes.Count == 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.Scope)}.{nameof(ScopeAuthorizationOptions.ClaimTypes)} cannot be empty; no scope could ever be read.");
        }

        if (options.Scope.ClaimTypes.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.Scope)}.{nameof(ScopeAuthorizationOptions.ClaimTypes)} cannot contain a blank claim type.");
        }

        if (string.IsNullOrEmpty(options.Scope.Separators))
        {
            return ValidateOptionsResult.Fail($"{nameof(AccessAuthorizationOptions.Scope)}.{nameof(ScopeAuthorizationOptions.Separators)} cannot be empty; a claim value could not be split.");
        }

        return ValidateOptionsResult.Success;
    }
}
