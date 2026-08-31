using ApricotFramework.AccessAuthorization.Examples.Web.Model;

namespace ApricotFramework.AccessAuthorization.Examples.Web.Access;

/// <summary>
/// Stands in for whatever a real service assigns accesses from — a table, a role expansion, a
/// directory. The library never learns which.
/// </summary>
/// <remarks>
/// Keyed on the whole subject rather than its id, which is the point of a composite identity: the
/// same person can hold different accesses in different organisations. The organisation is read
/// through <see cref="OrganizationSubject"/> rather than by reaching into the attribute bag here.
/// </remarks>
public sealed class InMemoryAccessStore : IAccessStore
{
    /// <summary>
    /// Stands for an assignment that does not depend on the organisation.
    /// </summary>
    private const string AnyOrganization = "*";

    /// <summary>
    /// Who has been assigned what, by user id and organisation.
    /// </summary>
    private static readonly Dictionary<(string User, string Organization), string[]> Assignments = new()
    {
        [("reader", AnyOrganization)] = [OrderAccesses.Read],
        [("editor", "acme")] = [OrderAccesses.Read, OrderAccesses.Edit],
        [("editor", AnyOrganization)] = [OrderAccesses.Read],
    };

    /// <inheritdoc />
    public Task<IReadOnlySet<string>> GetAccessesAsync(AccessSubject subject, CancellationToken cancellationToken = default)
    {
        var scoped = OrganizationSubject.TryFrom(subject, out var organization)
            ? Assignments.GetValueOrDefault((organization!.UserId, organization.OrganizationId))
            : null;

        var assigned = scoped ?? Assignments.GetValueOrDefault((subject.Id, AnyOrganization)) ?? [];

        return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(assigned, StringComparer.Ordinal));
    }
}
