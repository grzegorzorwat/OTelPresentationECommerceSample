using Carts.Pricing;
using Carts.ShoppingCarts.Products;
using Core.Commands;
using Core.Marten.Events;
using Core.Marten.Repository;
using MediatR;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Carts.ShoppingCarts.AddingProduct;

public record AddProduct(
    Guid CartId,
    ProductItem ProductItem
): ICommand
{
    public static AddProduct Create(Guid cartId, ProductItem productItem)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(cartId));

        return new AddProduct(cartId, productItem);
    }
}

internal class HandleAddProduct:
    ICommandHandler<AddProduct>
{
    private readonly IMartenRepository<ShoppingCart> cartRepository;
    private readonly IProductPriceCalculator productPriceCalculator;
    private readonly IMartenAppendScope scope;

    public HandleAddProduct(
        IMartenRepository<ShoppingCart> cartRepository,
        IProductPriceCalculator productPriceCalculator,
        IMartenAppendScope scope
    )
    {
        this.cartRepository = cartRepository;
        this.productPriceCalculator = productPriceCalculator;
        this.scope = scope;
    }

    public async Task<Unit> Handle(AddProduct command, CancellationToken cancellationToken)
    {
        Baggage.SetBaggage("ECommerce.CartId", command.CartId.ToString());
        var (cartId, productItem) = command;

        await scope.Do((expectedVersion) =>
            cartRepository.GetAndUpdate(
                cartId,
                cart => cart.AddProduct(productPriceCalculator, productItem),
                expectedVersion,
                cancellationToken
            )
        );
        return Unit.Value;
    }
}
