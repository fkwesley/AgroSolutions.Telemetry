using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Tests.UnitTests.Domain.Entities
{
    public class OrderTests
    {
        #region Constructor & Basic Properties Tests

        [Fact]
        public void Order_WhenCreated_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var order = CreateValidOrder();

            // Assert
            order.Should().NotBeNull();
            order.UserId.Should().Be("user-123");
            order.UserEmail.Should().Be("user@test.com");
            order.Status.Should().Be(OrderStatus.PendingPayment);
            order.PaymentMethod.Should().Be(PaymentMethod.Pix);
            order.ListOfGames.Should().BeEmpty();
            order.TotalPrice.Should().Be(0);
            order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            order.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void TotalPrice_WithNoGames_ShouldReturnZero()
        {
            // Arrange
            var order = CreateValidOrder();

            // Act
            var totalPrice = order.TotalPrice;

            // Assert
            totalPrice.Should().Be(0);
        }

        [Fact]
        public void TotalPrice_WithMultipleGames_ShouldSumAllPrices()
        {
            // Arrange
            var order = CreateValidOrder();
            order.ListOfGames = new List<Game>
            {
                new Game { GameId = 1, Name = "Game 1", Price = 59.99 },
                new Game { GameId = 2, Name = "Game 2", Price = 89.99 },
                new Game { GameId = 3, Name = "Game 3", Price = 120.00}
            };

            // Act
            var totalPrice = order.TotalPrice;

            // Assert
            totalPrice.Should().Be(269.98);
        }

        #endregion

        #region Status Change Tests

        [Fact]
        public void Status_WhenChanged_ShouldUpdateSuccessfully()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 1;

            // Act
            order.Status = OrderStatus.Paid;

            // Assert
            order.Status.Should().Be(OrderStatus.Paid);
        }

        [Fact]
        public void Status_WhenChanged_ShouldAddOrderStatusChangedEvent()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 1;
            order.ClearDomainEvents();

            // Act
            order.Status = OrderStatus.Paid;

            // Assert
            order.DomainEvents.Should().HaveCount(1);
            var statusEvent = order.DomainEvents.First() as OrderStatusChangedEvent;
            statusEvent.Should().NotBeNull();
            statusEvent!.OrderId.Should().Be(1);
            statusEvent.OldStatus.Should().Be(OrderStatus.PendingPayment);
            statusEvent.NewStatus.Should().Be(OrderStatus.Paid);
        }

        [Fact]
        public void Status_WhenChangedFromReleased_ShouldThrowBusinessException()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 1;
            order.Status = OrderStatus.Released;
            order.ClearDomainEvents();

            // Act
            Action act = () => order.Status = OrderStatus.Cancelled;

            // Assert
            act.Should().Throw<BusinessException>()
                .WithMessage("Cannot change the status of an order that is already released.");
        }

        [Fact]
        public void Status_WhenChangedToSameValue_ShouldNotAddEvent()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 1;
            order.ClearDomainEvents();
            var currentStatus = order.Status;

            // Act
            order.Status = currentStatus;

            // Assert
            order.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void Status_WhenOrderIdIsZero_ShouldNotAddEvent()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 0;

            // Act
            order.Status = OrderStatus.PendingPayment;

            // Assert
            order.DomainEvents.Should().BeEmpty();
        }

        #endregion

        #region Payment Method Details Tests

        [Fact]
        public void PaymentMethodDetails_WhenPaymentMethodIsPix_ShouldAllowNull()
        {
            // Arrange
            var order = CreateValidOrder(PaymentMethod.Pix);

            // Act
            Action act = () => order.PaymentMethodDetails = null;

            // Assert
            act.Should().NotThrow();
            order.PaymentMethodDetails.Should().BeNull();
        }

        [Fact]
        public void PaymentMethodDetails_WhenPaymentMethodIsCreditCard_AndDetailsAreNull_ShouldThrowBusinessException()
        {
            // Arrange
            var order = CreateValidOrder(PaymentMethod.CreditCard);

            // Act
            Action act = () => order.PaymentMethodDetails = null;

            // Assert
            act.Should().Throw<BusinessException>()
                .WithMessage("Payment method details are required for credit or debit card payments.");
        }

        [Fact]
        public void PaymentMethodDetails_WithValidCardNumber_ShouldSetSuccessfully()
        {
            // Arrange
            var order = CreateValidOrder(PaymentMethod.CreditCard);

            // Act
            Action act = () => order.PaymentMethodDetails = new PaymentMethodDetails()
            {
                CardNumber = "4111111111111111",
                ExpiryDate = DateTime.UtcNow.AddYears(2).ToString("yyyy-MM"),
                Cvv = "123",
                CardHolder = "John Doe"
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void PaymentMethodDetails_WithInvalidCardNumber_ShouldThrowBusinessException()
        {
            // Arrange
            var order = CreateValidOrder(PaymentMethod.CreditCard);

            // Act
            Action act = () => order.PaymentMethodDetails = new PaymentMethodDetails()
            {
                CardNumber = "123",
                ExpiryDate = DateTime.UtcNow.AddYears(2).ToString("yyyy-MM"),
                Cvv = "123",
                CardHolder = "John Doe"
            };

            // Assert
            act.Should().Throw<BusinessException>().WithMessage("Invalid card number.");
        }

        [Fact]
        public void PaymentMethodDetails_WithExpiredCard_ShouldThrowBusinessException()
        {
            // Arrange
            var order = CreateValidOrder(PaymentMethod.CreditCard);

            // Act
            Action act = () => order.PaymentMethodDetails = new PaymentMethodDetails()
            {
                CardNumber = "340000000000009",
                ExpiryDate = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM"),
                Cvv = "123",
                CardHolder = "John Doe"
            };

            // Assert
            act.Should().Throw<BusinessException>()
                .WithMessage("The card has already expired or is invalid. Provide a new card");
        }

        #endregion

        #region Card Validation Tests

        [Theory]
        [InlineData("4111111111111111", true)] // Visa válido
        [InlineData("5500000000000004", true)] // Mastercard válido
        [InlineData("340000000000009", true)] // Amex válido (15 dígitos)
        [InlineData("6011000000000004", true)] // Discover válido (16 dígitos)
        [InlineData("3566000000000000000", true)] // JCB válido (19 dígitos)
        [InlineData("123", false)] // Muito curto
        [InlineData("12345678901234567890", false)] // Muito longo
        [InlineData("", false)] // Vazio
        [InlineData(null, false)] // Null
        [InlineData("   ", false)] // Apenas espaços
        public void IsValidCardNumber_ShouldValidateCorrectly(string cardNumber, bool expectedResult)
        {
            // Arrange
            var order = CreateValidOrder();

            // Act
            var result = order.IsValidCardNumber(cardNumber);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("invalid", false)] // Formato inválido
        [InlineData("", false)] // Vazio
        public void IsValidExpiryDate_ShouldValidateCorrectly(string expiryDate, bool expectedResult)
        {
            // Arrange
            var order = CreateValidOrder();

            // Act
            var result = order.IsValidExpiryDate(expiryDate);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void IsValidExpiryDate_WithFutureDistantDate_ShouldBeValid()
        {
            // Arrange
            var order = CreateValidOrder();

            // Act
            var result = order.IsValidExpiryDate(DateTime.UtcNow.AddYears(5).ToString("yyyy-MM"));

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidExpiryDate_WithPastDate_ShouldBeInvalid()
        {
            // Arrange
            var order = CreateValidOrder();

            // Act
            var result = order.IsValidExpiryDate(DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM"));

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidExpiryDate_WithWrongFormat_ShouldBeInvalid()
        {
            // Arrange
            var order = CreateValidOrder();

            // Act
            var result = order.IsValidExpiryDate("12-2025");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidExpiryDate_WithCurrentMonthLastDay_ShouldBeValid()
        {
            // Arrange
            var order = CreateValidOrder();
            var currentYearMonth = DateTime.UtcNow.ToString("yyyy-MM");

            // Act
            var result = order.IsValidExpiryDate(currentYearMonth);

            // Assert - Deve ser válido até o último dia do mês atual
            result.Should().BeTrue();
        }

        #endregion

        #region Domain Events Tests

        [Fact]
        public void MarkAsCreated_ShouldAddOrderCreatedEvent()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 1;
            order.ClearDomainEvents();

            // Act
            order.MarkAsCreated();

            // Assert
            order.DomainEvents.Should().HaveCount(2);
            
            var createdEvent = order.DomainEvents.FirstOrDefault(e => e is OrderCreatedEvent) as OrderCreatedEvent;
            createdEvent.Should().NotBeNull();
            createdEvent!.OrderId.Should().Be(1);
            createdEvent.UserEmail.Should().Be("user@test.com");
        }

        [Fact]
        public void MarkAsCreated_ShouldAddPaymentMethodSetEvent()
        {
            // Arrange
            var order = CreateValidOrder();
            order.OrderId = 1;
            order.ListOfGames = new List<Game>
            {
                new Game { GameId = 1, Name = "Game 1", Price = 59.99 }
            };
            order.ClearDomainEvents();

            // Act
            order.MarkAsCreated();

            // Assert
            order.DomainEvents.Should().HaveCount(2);
            
            var paymentEvent = order.DomainEvents.FirstOrDefault(e => e is PaymentMethodSetEvent) as PaymentMethodSetEvent;
            paymentEvent.Should().NotBeNull();
            paymentEvent!.OrderId.Should().Be(1);
            paymentEvent.PaymentMethod.Should().Be(PaymentMethod.Pix);
            paymentEvent.TotalPrice.Should().Be(59.99);
        }

        #endregion

        #region Helper Methods

        private Order CreateValidOrder(PaymentMethod paymentMethod = PaymentMethod.Pix)
        {
            return new Order
            {
                UserId = "user-123",
                UserEmail = "user@test.com",
                Status = OrderStatus.PendingPayment,
                PaymentMethod = paymentMethod,
                ListOfGames = new List<Game>()
            };
        }

        #endregion
    }
}
