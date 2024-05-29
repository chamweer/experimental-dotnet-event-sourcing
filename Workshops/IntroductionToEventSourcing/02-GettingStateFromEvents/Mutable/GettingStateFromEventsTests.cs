using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.GettingStateFromEvents.Mutable;
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
public class PricedProductItem
{
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;
}

// ENTITY
public class ShoppingCart
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public ShoppingCartStatus Status { get; set; }
    public IList<PricedProductItem> ProductItems { get; set; } = new List<PricedProductItem>();
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CanceledAt { get; set; }

    public void Evolve(object @event)
    {
        switch (@event)
        {
            case ShoppingCartOpened e:
                Apply(e);
                break;
            case ProductItemAddedToShoppingCart e:
                Apply(e);
                break;
            case ProductItemRemovedFromShoppingCart e:
                Apply(e);
                break;
            case ShoppingCartConfirmed e:
                Apply(e);
                break;
            case ShoppingCartCanceled e:
                Apply(e);
                break;
            default:
                throw new InvalidOperationException($"Unsupported event type: {@event.GetType().Name}");
        }
    }

    private void Apply(ShoppingCartOpened @event)
    {
        Id = @event.ShoppingCartId;
        ClientId = @event.ClientId;
        Status = ShoppingCartStatus.Pending;
    }

    private void Apply(ProductItemAddedToShoppingCart @event)
    {
        ThrowIfCartNotCreated();

        var existingProduct = ProductItems.SingleOrDefault(p => p.ProductId == @event.ProductItem.ProductId);
        if (existingProduct is null)
        {
            ProductItems.Add(@event.ProductItem);
            return;
        }

        existingProduct.Quantity += @event.ProductItem.Quantity;
    }

    private void Apply(ProductItemRemovedFromShoppingCart @event)
    {
        ThrowIfCartNotCreated();

        var existingProduct = ProductItems.SingleOrDefault(p => p.ProductId == @event.ProductItem.ProductId) ??
            throw new InvalidOperationException($"Product with id {@event.ProductItem.ProductId} not found in the shopping cart.");

        if (existingProduct.Quantity < @event.ProductItem.Quantity)
        {
            throw new InvalidOperationException($"Cannot remove {@event.ProductItem.Quantity} items of product with id {@event.ProductItem.ProductId} from the shopping cart. There are only {existingProduct.Quantity} items.");
        }

        if (existingProduct.Quantity == @event.ProductItem.Quantity)
        {
            ProductItems.Remove(existingProduct);
        }

        existingProduct.Quantity -= @event.ProductItem.Quantity;
    }

    private void Apply(ShoppingCartConfirmed @event)
    {
        ThrowIfCartNotCreated();

        ConfirmedAt = @event.ConfirmedAt;
        Status = ShoppingCartStatus.Confirmed;
    }

    private void Apply(ShoppingCartCanceled @event)
    {
        ThrowIfCartNotCreated();

        CanceledAt = @event.CanceledAt;
        Status = ShoppingCartStatus.Canceled;
    }

    private void ThrowIfCartNotCreated()
    {
        if (Id == Guid.Empty)
        {
            throw new InvalidOperationException("Shopping cart not created.");
        }
    }
}

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
        ShoppingCart cart = new();
        foreach (var @event in events)
        {
            cart.Evolve(@event);
        }

        return cart;
    }

    [Fact]
    [Trait("Category", "SkipCI")]
    public void GettingState_ForSequenceOfEvents_ShouldSucceed()
    {
        var shoppingCartId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var shoesId = Guid.NewGuid();
        var tShirtId = Guid.NewGuid();
        var twoPairsOfShoes =
            new PricedProductItem
            {
                ProductId = shoesId,
                Quantity = 2,
                UnitPrice = 100
            };
        var pairOfShoes =
            new PricedProductItem
            {
                ProductId = shoesId,
                Quantity = 1,
                UnitPrice = 100
            };
        var tShirt =
            new PricedProductItem
            {
                ProductId = tShirtId,
                Quantity = 1,
                UnitPrice = 50
            };

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

        shoppingCart.ProductItems[0].ProductId.Should().Be(shoesId);
        shoppingCart.ProductItems[0].Quantity.Should().Be(pairOfShoes.Quantity);
        shoppingCart.ProductItems[0].UnitPrice.Should().Be(pairOfShoes.UnitPrice);

        shoppingCart.ProductItems[1].ProductId.Should().Be(tShirtId);
        shoppingCart.ProductItems[1].Quantity.Should().Be(tShirt.Quantity);
        shoppingCart.ProductItems[1].UnitPrice.Should().Be(tShirt.UnitPrice);
    }
}
