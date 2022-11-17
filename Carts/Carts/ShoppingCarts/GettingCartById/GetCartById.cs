using Core.Queries;
using Marten;
using OpenTelemetry;

namespace Carts.ShoppingCarts.GettingCartById;

public record GetCartById(
    Guid CartId
): IQuery<ShoppingCartDetails>
{
    public static GetCartById Create(Guid cartId)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(cartId));

        return new GetCartById(cartId);
    }
}

internal class HandleGetCartById:
    IQueryHandler<GetCartById, ShoppingCartDetails?>
{
    private readonly IQuerySession querySession;

    public HandleGetCartById(IQuerySession querySession)
    {
        this.querySession = querySession;
    }

    public Task<ShoppingCartDetails?> Handle(GetCartById query, CancellationToken cancellationToken)
    {
        Baggage.SetBaggage("ECommerce.CartId", query.CartId.ToString());
        return querySession.LoadAsync<ShoppingCartDetails>(query.CartId, cancellationToken);
    }
}
