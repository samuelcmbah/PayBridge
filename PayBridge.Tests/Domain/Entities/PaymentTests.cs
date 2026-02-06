using FluentAssertions;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using PayBridge.Domain.Exceptions;
using PayBridge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Tests.Domain.Entities
{
    public class PaymentTests
    {
        #region Test Helper Methods

        /// <summary>
        /// Helper method to create a valid payment for testing
        /// This reduces code duplication across tests
        /// </summary>
        private Payment CreateValidPayment(
            decimal amount = 1000m,
            string currency = "NGN",
            PaymentProvider provider = PaymentProvider.Paystack,
            PaymentPurpose purpose = PaymentPurpose.ProductCheckout)
        {
            var money = Money.Create(amount, currency);
            var email = Email.Create("user@example.com");
            var redirectUrl = Url.Create("https://example.com/callback");
            var notificationUrl = Url.Create("https://example.com/webhook");

            return new Payment(
                provider,
                purpose,
                money,
                email,
                "TestApp",
                "ORDER-123",
                redirectUrl,
                notificationUrl
            );
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreatePayment()
        {
            // Arrange
            var amount = Money.Create(1000m, "NGN");
            var email = Email.Create("user@example.com");
            var redirectUrl = Url.Create("https://example.com/callback");
            var notificationUrl = Url.Create("https://example.com/webhook");

            // Act
            var payment = new Payment(
                PaymentProvider.Paystack,
                PaymentPurpose.ProductCheckout,
                amount,
                email,
                "SportStore",
                "ORDER-123",
                redirectUrl,
                notificationUrl
            );

            // Assert
            payment.Id.Should().NotBeEmpty();
            payment.Reference.Should().NotBeNull();
            payment.Provider.Should().Be(PaymentProvider.Paystack);
            payment.Purpose.Should().Be(PaymentPurpose.ProductCheckout);
            payment.Amount.Should().Be(amount);
            payment.ExternalUserId.Should().Be(email);
            payment.AppName.Should().Be("SportStore");
            payment.ExternalReference.Should().Be("ORDER-123");
            payment.RedirectUrl.Should().Be(redirectUrl);
            payment.NotificationUrl.Should().Be(notificationUrl);
            payment.Status.Should().Be(PaymentStatus.Pending);
            payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            payment.VerifiedAt.Should().BeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidAppName_ShouldThrowPaymentStateException(string appName)
        {
            // Arrange
            var amount = Money.Create(1000m);
            var email = Email.Create("user@example.com");
            var redirectUrl = Url.Create("https://example.com/callback");
            var notificationUrl = Url.Create("https://example.com/webhook");

            // Act
            var act = () => new Payment(
                PaymentProvider.Paystack,
                PaymentPurpose.ProductCheckout,
                amount,
                email,
                appName,
                "ORDER-123",
                redirectUrl,
                notificationUrl
            );

            // Assert
            act.Should().Throw<PaymentStateException>()
                .WithMessage("AppName cannot be empty")
                .And.ErrorCode.Should().Be("INVALID_APP_NAME");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_WithInvalidExternalReference_ShouldThrowPaymentStateException(string externalRef)
        {
            // Arrange
            var amount = Money.Create(1000m);
            var email = Email.Create("user@example.com");
            var redirectUrl = Url.Create("https://example.com/callback");
            var notificationUrl = Url.Create("https://example.com/webhook");

            // Act
            var act = () => new Payment(
                PaymentProvider.Paystack,
                PaymentPurpose.ProductCheckout,
                amount,
                email,
                "TestApp",
                externalRef,
                redirectUrl,
                notificationUrl
            );

            // Assert
            act.Should().Throw<PaymentStateException>()
                .WithMessage("ExternalReference cannot be empty")
                .And.ErrorCode.Should().Be("INVALID_EXTERNAL_REFERENCE");
        }

        [Fact]
        public void Constructor_ShouldGenerateUniqueReference()
        {
            // Act
            var payment1 = CreateValidPayment();
            var payment2 = CreateValidPayment();

            // Assert
            payment1.Reference.Should().NotBe(payment2.Reference);
            payment1.Reference.Value.Should().StartWith("PB_");
            payment2.Reference.Value.Should().StartWith("PB_");
        }

        [Fact]
        public void Constructor_ShouldTrimAppNameAndExternalReference()
        {
            // Arrange
            var amount = Money.Create(1000m);
            var email = Email.Create("user@example.com");
            var redirectUrl = Url.Create("https://example.com/callback");
            var notificationUrl = Url.Create("https://example.com/webhook");

            // Act
            var payment = new Payment(
                PaymentProvider.Paystack,
                PaymentPurpose.ProductCheckout,
                amount,
                email,
                "  TestApp  ",
                "  ORDER-123  ",
                redirectUrl,
                notificationUrl
            );

            // Assert
            payment.AppName.Should().Be("TestApp");
            payment.ExternalReference.Should().Be("ORDER-123");
        }

        #endregion

        #region ProcessSuccessfulPayment Tests

        [Fact]
        public void ProcessSuccessfulPayment_WithMatchingAmount_ShouldMarkAsSuccessful()
        {
            // Arrange
            var payment = CreateValidPayment(amount: 1000m);
            var receivedAmount = Money.Create(1000m, "NGN");

            // Act
            payment.ProcessSuccessfulPayment(receivedAmount);

            // Assert
            payment.Status.Should().Be(PaymentStatus.Success);
            payment.VerifiedAt.Should().NotBeNull();
            payment.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void ProcessSuccessfulPayment_WithMismatchedAmount_ShouldMarkAsFailedAndThrow()
        {
            // Arrange
            var payment = CreateValidPayment(amount: 1000m);
            var receivedAmount = Money.Create(500m, "NGN");

            // Act
            var act = () => payment.ProcessSuccessfulPayment(receivedAmount);

            // Assert
            act.Should().Throw<PaymentAmountMismatchException>()
                .WithMessage("Expected ₦1,000.00, received ₦500.00");

            payment.Status.Should().Be(PaymentStatus.Failed);
            payment.VerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public void ProcessSuccessfulPayment_OnAlreadyProcessedPayment_ShouldThrowPaymentStateException()
        {
            // Arrange
            var payment = CreateValidPayment();
            var receivedAmount = Money.Create(1000m, "NGN");

            // First processing
            payment.ProcessSuccessfulPayment(receivedAmount);

            // Act - Try to process again
            var act = () => payment.ProcessSuccessfulPayment(receivedAmount);

            // Assert
            act.Should().Throw<PaymentStateException>()
                .WithMessage("Payment already processed with status: Success")
                .And.ErrorCode.Should().Be("ALREADY_PROCESSED");
        }

        #endregion

        #region MarkInitializationFailed Tests

        [Fact]
        public void MarkInitializationFailed_OnPendingPayment_ShouldMarkAsFailed()
        {
            // Arrange
            var payment = CreateValidPayment();

            // Act
            payment.MarkInitializationFailed();

            // Assert
            payment.Status.Should().Be(PaymentStatus.Failed);
            payment.VerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkInitializationFailed_OnNonPendingPayment_ShouldThrowPaymentStateException()
        {
            // Arrange
            var payment = CreateValidPayment();
            var receivedAmount = Money.Create(1000m, "NGN");
            payment.ProcessSuccessfulPayment(receivedAmount);

            // Act
            var act = () => payment.MarkInitializationFailed();

            // Assert
            act.Should().Throw<PaymentStateException>()
                .WithMessage("Cannot mark as initialization failed. Current status: Success")
                .And.ErrorCode.Should().Be("INVALID_STATE_TRANSITION");
        }

        #endregion

        #region Integration/Scenario Tests

        [Fact]
        public void Payment_FullSuccessfulFlow_ShouldWork()
        {
            // Arrange - Create payment
            var payment = CreateValidPayment(amount: 5000m);

            // Assert initial state
            payment.Status.Should().Be(PaymentStatus.Pending);
            payment.VerifiedAt.Should().BeNull();

            // Act - Process successful payment
            var receivedAmount = Money.Create(5000m, "NGN");
            payment.ProcessSuccessfulPayment(receivedAmount);

            // Assert final state
            payment.Status.Should().Be(PaymentStatus.Success);
            payment.VerifiedAt.Should().NotBeNull();
        }

        [Fact]
        public void Payment_FailedInitializationFlow_ShouldWork()
        {
            // Arrange - Create payment
            var payment = CreateValidPayment();

            // Assert initial state
            payment.Status.Should().Be(PaymentStatus.Pending);

            // Act - Mark initialization as failed (e.g., gateway error)
            payment.MarkInitializationFailed();

            // Assert final state
            payment.Status.Should().Be(PaymentStatus.Failed);
            payment.VerifiedAt.Should().NotBeNull();
        }

        #endregion
    }
}
