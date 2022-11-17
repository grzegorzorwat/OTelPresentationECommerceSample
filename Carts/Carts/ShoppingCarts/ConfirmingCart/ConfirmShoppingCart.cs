using Core.Commands;
using Core.Marten.Events;
using Core.Marten.Repository;
using MediatR;
using OpenTelemetry;

namespace Carts.ShoppingCarts.ConfirmingCart;

public record ConfirmShoppingCart(
    Guid CartId
): ICommand
{
    public static ConfirmShoppingCart Create(Guid cartId)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(cartId));

        return new ConfirmShoppingCart(cartId);
    }
}

internal class HandleConfirmShoppingCart:
    ICommandHandler<ConfirmShoppingCart>
{
    private readonly IMartenRepository<ShoppingCart> cartRepository;
    private readonly IMartenAppendScope scope;

    public HandleConfirmShoppingCart(
        IMartenRepository<ShoppingCart> cartRepository,
        IMartenAppendScope scope
    )
    {
        this.cartRepository = cartRepository;
        this.scope = scope;
    }

    public async Task<Unit> Handle(ConfirmShoppingCart command, CancellationToken cancellationToken)
    {
        Baggage.SetBaggage("ECommerce.CartId", command.CartId.ToString());

        await scope.Do((expectedVersion) =>
            cartRepository.GetAndUpdate(
                command.CartId,
                cart => cart.Confirm(),
                expectedVersion,
                cancellationToken
            )
        );
        return Unit.Value;
    }
}
