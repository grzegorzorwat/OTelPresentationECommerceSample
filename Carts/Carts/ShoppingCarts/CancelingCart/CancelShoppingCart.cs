using Core.Commands;
using Core.Marten.Events;
using Core.Marten.Repository;
using MediatR;
using OpenTelemetry;

namespace Carts.ShoppingCarts.CancelingCart;

public record CancelShoppingCart(
    Guid CartId
): ICommand
{
    public static CancelShoppingCart Create(Guid cartId)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(cartId));

        return new CancelShoppingCart(cartId);
    }
}

internal class HandleCancelShoppingCart:
    ICommandHandler<CancelShoppingCart>
{
    private readonly IMartenRepository<ShoppingCart> cartRepository;
    private readonly IMartenAppendScope scope;

    public HandleCancelShoppingCart(
        IMartenRepository<ShoppingCart> cartRepository,
        IMartenAppendScope scope
    )
    {
        this.cartRepository = cartRepository;
        this.scope = scope;
    }

    public async Task<Unit> Handle(CancelShoppingCart command, CancellationToken cancellationToken)
    {
        Baggage.SetBaggage("ECommerce.CartId", command.CartId.ToString());
        await scope.Do((expectedVersion) =>
            cartRepository.GetAndUpdate(
                command.CartId,
                cart => cart.Cancel(),
                expectedVersion,
                cancellationToken
            )
        );
        return Unit.Value;
    }
}
