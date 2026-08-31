using ApricotFramework.AccessAuthorization.AspNetCore.Attributes;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Attributes;
using ApricotFramework.AccessAuthorization.Examples.Web.Grpc;
using ApricotFramework.AccessAuthorization.Examples.Web.Model;
using Grpc.Core;

namespace ApricotFramework.AccessAuthorization.Examples.Web.Grpc;

/// <summary>
/// The gRPC half of the demonstration, and the point of the whole exercise: the same attributes work
/// here, where an MVC filter never ran.
/// </summary>
public sealed class OrdersGrpcService : Orders.OrdersBase
{
    /// <summary>
    /// Reads a order, gated on a token scope — the service-to-service case the predecessor had to
    /// check by hand inside every method body.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="context">The call context.</param>
    /// <returns>The order.</returns>
    [AuthorizeAllScopes("sales.read")]
    public override Task<GetOrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetOrderReply { Id = request.Id, Title = $"Order {request.Id}" });
    }

    /// <summary>
    /// Deletes a order, gated on an assigned access rather than a scope.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="context">The call context.</param>
    /// <returns>The result.</returns>
    [AuthorizeAnyAccess(OrderAccesses.Delete)]
    public override Task<DeleteOrderReply> DeleteOrder(DeleteOrderRequest request, ServerCallContext context)
    {
        return Task.FromResult(new DeleteOrderReply { Deleted = true });
    }
}
