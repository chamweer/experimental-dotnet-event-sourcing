using FluentAssertions;
using ImTools;
using Xunit;

namespace IntroductionToEventSourcing.GettingStateFromEvents.Immutable;

using static ShoppingCartEvent;

// EVENTS
public abstract record ShoppingCartEvent
{
    public record ShoppingCartOpened(
        Guid ShoppingCartId,
        Guid ClientId
    ): ShoppingCartEvent;

    public record ProductItemAddedToShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    ): ShoppingCartEvent;

    public record ProductItemRemovedFromShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    ): ShoppingCartEvent;

    public record ShoppingCartConfirmed(
        Guid ShoppingCartId,
        DateTime ConfirmedAt
    ): ShoppingCartEvent;

    public record ShoppingCartCanceled(
        Guid ShoppingCartId,
        DateTime CanceledAt
    ): ShoppingCartEvent;

    // This won't allow external inheritance
    private ShoppingCartEvent() { }
}

// VALUE OBJECTS
public record PricedProductItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

// ENTITY
public record ShoppingCart(
    Guid Id,
    Guid ClientId,
    ShoppingCartStatus Status,
    PricedProductItem[] ProductItems,
    DateTime? ConfirmedAt = null,
    DateTime? CanceledAt = null
)
{
    public static ShoppingCart Empty = new(Guid.Empty, Guid.Empty, ShoppingCartStatus.Pending, []);
};

public enum ShoppingCartStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 4
}

public class GettingStateFromEventsTests
{
    // 1. Add logic here
    private static ShoppingCart GetShoppingCart(IEnumerable<ShoppingCartEvent> events)
    {
        var cart = ShoppingCart.Empty;
        foreach (var @event in events)
        {
            cart = @event switch
            {
                ShoppingCartOpened(Guid cartId, Guid clientId) => cart with { Id = cartId, ClientId = clientId },
                ProductItemAddedToShoppingCart(_, PricedProductItem product) => AddProduct(product),
                ProductItemRemovedFromShoppingCart(_, PricedProductItem product) => RemoveProduct(product),
                ShoppingCartConfirmed(_, DateTime confirmedAt) => cart with { Status = ShoppingCartStatus.Confirmed, ConfirmedAt = confirmedAt },
                ShoppingCartCanceled(_, DateTime cancelledAt) => cart with { Status = ShoppingCartStatus.Canceled, CanceledAt = cancelledAt },
                _ => cart
            };
        }

        return cart;

        ShoppingCart AddProduct(PricedProductItem product)
        {
            var found = false;
            for (var i = 0; i < cart.ProductItems.Length; i++)
            {
                if (cart.ProductItems[i].ProductId != product.ProductId)
                {
                    continue;
                }

                found = true;
                cart.ProductItems[i] = new(cart.ProductItems[i].ProductId, cart.ProductItems[i].Quantity + product.Quantity, cart.ProductItems[i].UnitPrice);
            }

            if (!found)
            {
                return cart with { ProductItems = [.. cart.ProductItems, product] };
            }

            return cart;
        }

        ShoppingCart RemoveProduct(PricedProductItem product)
        {
            for (var i = 0; i < cart.ProductItems.Length; i++)
            {
                if (cart.ProductItems[i].ProductId != product.ProductId)
                {
                    continue;
                }

                cart.ProductItems[i] = new(cart.ProductItems[i].ProductId, cart.ProductItems[i].Quantity - product.Quantity, cart.ProductItems[i].UnitPrice);
            }

            return cart;
        }
    }

    [Fact]
    [Trait("Category", "SkipCI")]
    public void GettingState_ForSequenceOfEvents_ShouldSucceed()
    {
        var shoppingCartId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var shoesId = Guid.CreateVersion7();
        var tShirtId = Guid.CreateVersion7();
        var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
        var pairOfShoes = new PricedProductItem(shoesId, 1, 100);
        var tShirt = new PricedProductItem(tShirtId, 1, 50);

        var events = new ShoppingCartEvent[]
        {
            new ShoppingCartOpened(shoppingCartId, clientId),
            new ProductItemAddedToShoppingCart(shoppingCartId, twoPairsOfShoes),
            new ProductItemAddedToShoppingCart(shoppingCartId, tShirt),
            new ProductItemRemovedFromShoppingCart(shoppingCartId, pairOfShoes),
            new ShoppingCartConfirmed(shoppingCartId, DateTime.UtcNow),
            new ShoppingCartCanceled(shoppingCartId, DateTime.UtcNow)
        };

        var shoppingCart = GetShoppingCart(events);

        shoppingCart.Id.Should().Be(shoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.ProductItems.Should().HaveCount(2);
        shoppingCart.ProductItems[0].Should().Be(pairOfShoes);
        shoppingCart.ProductItems[1].Should().Be(tShirt);
    }
}
