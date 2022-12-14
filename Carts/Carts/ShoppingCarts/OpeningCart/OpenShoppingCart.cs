using Core.Commands;
using Core.Marten.Events;
using Core.Marten.Repository;
using MediatR;
using OpenTelemetry;

namespace Carts.ShoppingCarts.OpeningCart;

public record OpenShoppingCart(
    Guid CartId,
    Guid ClientId
): ICommand
{
    public static OpenShoppingCart Create(Guid? cartId, Guid? clientId)
    {
        if (cartId == null || cartId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(cartId));
        if (clientId == null || clientId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(clientId));

        return new OpenShoppingCart(cartId.Value, clientId.Value);
    }
}

internal class HandleOpenShoppingCart:
    ICommandHandler<OpenShoppingCart>
{
    private readonly IMartenRepository<ShoppingCart> cartRepository;
    private readonly IMartenAppendScope scope;

    public HandleOpenShoppingCart(
        IMartenRepository<ShoppingCart> cartRepository,
        IMartenAppendScope scope
    )
    {
        this.cartRepository = cartRepository;
        this.scope = scope;
    }

    public async Task<Unit> Handle(OpenShoppingCart command, CancellationToken cancellationToken)
    {
        Baggage.SetBaggage("ECommerce.CartId", command.CartId.ToString());
        var (cartId, clientId) = command;

        await scope.Do((_) =>
            cartRepository.Add(
                ShoppingCart.Open(cartId, clientId),
                cancellationToken
            )
        );
        return Unit.Value;
    }
}
