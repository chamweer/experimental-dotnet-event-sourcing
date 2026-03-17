using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.EventsDefinition;

// 1. Define your events and entity here

public class EventsDefinitionTests
{
    public interface IEvent;

    public record ShoppingCartOpened(Guid CartId, Guid UserId, DateTimeOffset OpenedAt): IEvent;

    public record ProductItemAddedToShoppingCart(Guid CartId, Guid ProductId, int Quantity): IEvent;

    public record ProductItemRemovedFromShoppingCart(Guid CartId, Guid ProductId): IEvent;

    public record ShoppingCartConfirmed(Guid CartId, DateTimeOffset ConfirmedAt): IEvent;

    public record ShoppingCartCancelled(Guid CartId, DateTimeOffset CancelledAt): IEvent;

    public record ShoppingCart(
        Guid Id,
        Guid UserId,
        Product[] Products,
        IStatus Status);

    public record Product(int Id, int Qty, decimal price)
    {
        public decimal TotalPrice => price * Qty;
    }

    public interface IStatus;
    public record Pending(DateTime OpenedAt): IStatus;
    public record Confirmed(DateTime ConfirmeddAt): IStatus;
    public record Cancelled(DateTime CancelleddAt): IStatus;

    [Fact]
    [Trait("Category", "SkipCI")]
    public void AllEventTypes_ShouldBeDefined()
    {
        var cartId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var events = new object[]
        {
            new ShoppingCartOpened(cartId, userId, DateTimeOffset.UtcNow),
            new ProductItemAddedToShoppingCart(cartId, productId, 5),
            new ProductItemRemovedFromShoppingCart(cartId, productId),
            new ShoppingCartConfirmed(cartId, DateTimeOffset.UtcNow),
            new ShoppingCartCancelled(cartId, DateTimeOffset.UtcNow)
        };

        const int expectedEventTypesCount = 5;
        events.Should().HaveCount(expectedEventTypesCount);
        events.GroupBy(e => e.GetType()).Should().HaveCount(expectedEventTypesCount);
    }
}
