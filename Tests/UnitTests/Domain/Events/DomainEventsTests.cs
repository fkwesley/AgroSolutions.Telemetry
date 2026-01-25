using Domain.Enums;
using Domain.Events;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Tests.UnitTests.Domain.Events
{
    public class DomainEventsTests
    {
        #region OrderCreatedEvent Tests

        [Fact]
        public void OrderCreatedEvent_ShouldBeCreatedWithCorrectProperties()
        {
            // Arrange
            var orderId = 123;
            var userEmail = "test@example.com";

            // Act
            var domainEvent = new OrderCreatedEvent(orderId, userEmail);

            // Assert
            domainEvent.OrderId.Should().Be(orderId);
            domainEvent.UserEmail.Should().Be(userEmail);
            domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void OrderCreatedEvent_WithDifferentOrders_ShouldNotBeEqual()
        {
            // Arrange
            var event1 = new OrderCreatedEvent(1, "user1@test.com");
            var event2 = new OrderCreatedEvent(2, "user2@test.com");

            // Act & Assert
            event1.OrderId.Should().NotBe(event2.OrderId);
            event1.UserEmail.Should().NotBe(event2.UserEmail);
        }

        #endregion

        #region OrderStatusChangedEvent Tests

        [Fact]
        public void OrderStatusChangedEvent_ShouldBeCreatedWithCorrectProperties()
        {
            // Arrange
            var orderId = 456;
            var oldStatus = OrderStatus.PendingPayment;
            var newStatus = OrderStatus.Processing;
            var userEmail = "user@test.com";

            // Act
            var domainEvent = new OrderStatusChangedEvent(orderId, oldStatus, newStatus, userEmail);

            // Assert
            domainEvent.OrderId.Should().Be(orderId);
            domainEvent.OldStatus.Should().Be(oldStatus);
            domainEvent.NewStatus.Should().Be(newStatus);
            domainEvent.UserEmail.Should().Be(userEmail);
            domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(OrderStatus.PendingPayment, OrderStatus.Paid)]
        [InlineData(OrderStatus.Processing, OrderStatus.Released)]
        [InlineData(OrderStatus.Paid, OrderStatus.Released)]
        [InlineData(OrderStatus.PendingPayment, OrderStatus.Cancelled)]
        public void OrderStatusChangedEvent_WithDifferentStatusTransitions_ShouldWork(OrderStatus oldStatus, OrderStatus newStatus)
        {
            // Arrange
            var orderId = 789;
            var userEmail = "user@test.com";

            // Act
            var domainEvent = new OrderStatusChangedEvent(orderId, oldStatus, newStatus, userEmail);

            // Assert
            domainEvent.OldStatus.Should().Be(oldStatus);
            domainEvent.NewStatus.Should().Be(newStatus);
        }

        #endregion

        #region PaymentMethodSetEvent Tests

        [Fact]
        public void PaymentMethodSetEvent_WithPixPayment_ShouldBeCreatedCorrectly()
        {
            // Arrange
            var orderId = 111;
            var paymentMethod = PaymentMethod.Pix;
            var totalAmount = 199.99;
            var userEmail = "pix@test.com";

            // Act
            var domainEvent = new PaymentMethodSetEvent(orderId, paymentMethod, totalAmount, userEmail, null);

            // Assert
            domainEvent.OrderId.Should().Be(orderId);
            domainEvent.PaymentMethod.Should().Be(paymentMethod);
            domainEvent.TotalPrice.Should().Be(totalAmount);
            domainEvent.UserEmail.Should().Be(userEmail);
            domainEvent.PaymentDetails.Should().BeNull();
            domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PaymentMethodSetEvent_WithCreditCardPayment_ShouldIncludeDetails()
        {
            // Arrange
            var orderId = 222;
            var paymentMethod = PaymentMethod.CreditCard;
            var totalAmount = 299.99;
            var userEmail = "card@test.com";
            var paymentDetails = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            // Act
            var domainEvent = new PaymentMethodSetEvent(orderId, paymentMethod, totalAmount, userEmail, paymentDetails);

            // Assert
            domainEvent.OrderId.Should().Be(orderId);
            domainEvent.PaymentMethod.Should().Be(paymentMethod);
            domainEvent.TotalPrice.Should().Be(totalAmount);
            domainEvent.UserEmail.Should().Be(userEmail);
            domainEvent.PaymentDetails.Should().NotBeNull();
            domainEvent.PaymentDetails.Value.CardNumber.Should().Be("4111-1111-1111-1111");
        }

        [Theory]
        [InlineData(PaymentMethod.Pix)]
        [InlineData(PaymentMethod.CreditCard)]
        [InlineData(PaymentMethod.DebitCard)]
        public void PaymentMethodSetEvent_WithDifferentPaymentMethods_ShouldWork(PaymentMethod paymentMethod)
        {
            // Arrange
            var orderId = 333;
            var totalAmount = 149.99;
            var userEmail = "user@test.com";

            // Act
            var domainEvent = new PaymentMethodSetEvent(orderId, paymentMethod, totalAmount, userEmail, null);

            // Assert
            domainEvent.PaymentMethod.Should().Be(paymentMethod);
        }

        #endregion

        #region General Domain Event Tests

        [Fact]
        public void DomainEvents_OccurredOn_ShouldAlwaysBeInUtc()
        {
            // Arrange & Act
            var orderCreatedEvent = new OrderCreatedEvent(1, "user@test.com");
            var statusChangedEvent = new OrderStatusChangedEvent(1, OrderStatus.PendingPayment, OrderStatus.Paid, "user@test.com");
            var paymentSetEvent = new PaymentMethodSetEvent(1, PaymentMethod.Pix, 99.99, "user@test.com", null);

            // Assert
            orderCreatedEvent.OccurredOn.Kind.Should().Be(DateTimeKind.Utc);
            statusChangedEvent.OccurredOn.Kind.Should().Be(DateTimeKind.Utc);
            paymentSetEvent.OccurredOn.Kind.Should().Be(DateTimeKind.Utc);
        }

        #endregion
    }
}
