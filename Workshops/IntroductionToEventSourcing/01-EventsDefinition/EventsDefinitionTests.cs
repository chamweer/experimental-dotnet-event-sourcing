using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.EventsDefinition;

// 1. Define your events and entity here

internal sealed class ShoppingCart
{
    public string Id { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public Product[] products { get; set; } = [];

    public ShoppingCartStatus Status { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}

internal sealed record Product(string Id, int Quantity, decimal UnitPrice)
{
    public decimal TotalPrice => Quantity * UnitPrice;
}

internal enum ShoppingCartStatus
{
    Pending = 0,
    Confirmed,
    Cancelled
}

internal record CreateShoppingCart(string Id, string ClientId);
internal record AddProductToShoppingCart(string CartId, string ProductId, int Quantity, decimal unitPrice);
internal record RemoveProductFromShoppingCart(string CartId, string ProductId, int Quantity);
internal record ConfirmShoppingCart(string Id, DateTimeOffset ConfirmedAt);
internal record CancelShoppingCart(string Id, DateTimeOffset CancelledAt);

public class EventsDefinitionTests
{
    [Fact]
    [Trait("Category", "SkipCI")]
    public void AllEventTypes_ShouldBeDefined()
    {
        const string cartId = "1050";
        var events = new object[]
        {
            new CreateShoppingCart(cartId, "3080"),
            new AddProductToShoppingCart(cartId, "2050", 4, 35.99m),
            new RemoveProductFromShoppingCart(cartId, "2050", 2),
            new ConfirmShoppingCart(cartId, DateTimeOffset.UtcNow),
            new CancelShoppingCart(cartId, DateTimeOffset.UtcNow)
        };

        const int expectedEventTypesCount = 5;
        events.Should().HaveCount(expectedEventTypesCount);
        events.GroupBy(e => e.GetType()).Should().HaveCount(expectedEventTypesCount);
    }
}
