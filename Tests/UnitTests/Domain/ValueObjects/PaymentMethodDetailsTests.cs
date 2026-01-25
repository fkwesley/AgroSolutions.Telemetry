using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Tests.UnitTests.Domain.ValueObjects
{
    public class PaymentMethodDetailsTests
    {
        [Fact]
        public void PaymentMethodDetails_WithValidData_ShouldCreateSuccessfully()
        {
            // Arrange & Act
            var paymentDetails = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            // Assert
            paymentDetails.CardNumber.Should().Be("4111-1111-1111-1111");
            paymentDetails.CardHolder.Should().Be("John Doe");
            paymentDetails.ExpiryDate.Should().Be("2025-12");
            paymentDetails.Cvv.Should().Be("123");
        }

        [Theory]
        [InlineData("4111-1111-1111-1111")] // Visa
        [InlineData("5500-0000-0000-0004")] // Mastercard
        [InlineData("3400-0000-0000-0009")] // Amex
        [InlineData("6011-0000-0000-0004")] // Discover
        public void PaymentMethodDetails_WithValidCardNumberFormats_ShouldBeValid(string cardNumber)
        {
            // Arrange & Act
            var paymentDetails = new PaymentMethodDetails
            {
                CardNumber = cardNumber,
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            // Assert
            paymentDetails.CardNumber.Should().Be(cardNumber);
        }

        [Theory]
        [InlineData("2025-01")]
        [InlineData("2025-12")]
        [InlineData("2030-06")]
        [InlineData("2099-12")]
        public void PaymentMethodDetails_WithValidExpiryDateFormat_ShouldBeValid(string expiryDate)
        {
            // Arrange & Act
            var paymentDetails = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = expiryDate,
                Cvv = "123"
            };

            // Assert
            paymentDetails.ExpiryDate.Should().Be(expiryDate);
        }

        [Theory]
        [InlineData("000")]
        [InlineData("123")]
        [InlineData("456")]
        [InlineData("999")]
        public void PaymentMethodDetails_WithValidCvvFormat_ShouldBeValid(string cvv)
        {
            // Arrange & Act
            var paymentDetails = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = cvv
            };

            // Assert
            paymentDetails.Cvv.Should().Be(cvv);
        }

        [Fact]
        public void PaymentMethodDetails_Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var details1 = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            var details2 = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            var details3 = new PaymentMethodDetails
            {
                CardNumber = "5500-0000-0000-0004",
                CardHolder = "Jane Doe",
                ExpiryDate = "2026-01",
                Cvv = "456"
            };

            // Act & Assert
            details1.Equals(details2).Should().BeTrue();
            details1.Equals(details3).Should().BeFalse();
        }

        [Fact]
        public void PaymentMethodDetails_HashCode_ShouldBeConsistent()
        {
            // Arrange
            var details1 = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            var details2 = new PaymentMethodDetails
            {
                CardNumber = "4111-1111-1111-1111",
                CardHolder = "John Doe",
                ExpiryDate = "2025-12",
                Cvv = "123"
            };

            // Act & Assert
            details1.GetHashCode().Should().Be(details2.GetHashCode());
        }
    }
}
