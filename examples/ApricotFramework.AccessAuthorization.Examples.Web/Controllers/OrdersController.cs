using ApricotFramework.AccessAuthorization.AspNetCore.Attributes;
using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.Examples.Web.Access;
using ApricotFramework.AccessAuthorization.Examples.Web.Model;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApricotFramework.AccessAuthorization.Examples.Web.Controllers;

/// <summary>
/// The controller half of the demonstration. The attributes here are spelled exactly as they were in
/// the predecessor library.
/// </summary>
[ApiController]
[Route("/api/orders")]
public sealed class OrdersController : ControllerBase
{
    /// <summary>
    /// Reads a order. Gated on an assigned access, with no resource involved.
    /// </summary>
    /// <param name="id">The order id.</param>
    /// <returns>The order.</returns>
    [HttpGet("{id}")]
    [AuthorizeAnyAccess(OrderAccesses.Read)]
    public object Get(string id)
    {
        return new { Id = id, Title = $"Order {id}" };
    }

    /// <summary>
    /// Edits a order, checked against the order itself so the owner rule can apply.
    /// </summary>
    /// <param name="id">The order id.</param>
    /// <param name="authorization">The decision point.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    /// <remarks>
    /// Resource-level checks are imperative on purpose: deciding on a order's attributes needs the
    /// order loaded, which happens here rather than in an attribute.
    /// </remarks>
    [HttpPost("{id}")]
    [Authorize]
    public async Task<object> Edit(string id, [FromServices] IAccessAuthorization authorization, CancellationToken cancellationToken)
    {
        var subject = this.HttpContext.GetAccessSubject()!;
        var context = AccessContext.For(subject, OrderRepository.Describe(id));

        await authorization.RequireAnyAsync(context, [OrderAccesses.Edit], cancellationToken);

        return new { Id = id, Edited = true };
    }

    /// <summary>
    /// Lists what the caller may do to a order, for a user interface to render from.
    /// </summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The allowed accesses.</returns>
    [HttpGet("{id}/capabilities")]
    [Authorize]
    public async Task<object> Capabilities(string id, CancellationToken cancellationToken)
    {
        var evaluation = await this.HttpContext.GetAllowedAccessesAsync(OrderRepository.Describe(id), cancellationToken);

        return new { Id = id, Allowed = evaluation.Allowed.Order(StringComparer.Ordinal) };
    }

    /// <summary>
    /// Reachable without a token even though the class carries authorization, because the middleware
    /// honours this and not the library.
    /// </summary>
    /// <returns>A constant.</returns>
    [HttpGet("public")]
    [AuthorizeAnyAccess(OrderAccesses.Read)]
    [AllowAnonymous]
    public object Public()
    {
        return new { Anonymous = true };
    }
}
