using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PayBridge.Application.Common;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Application.Services;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using PayBridge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Tests.Application.Services
{
    public class PaymentServiceTests
    {
        #region Test Setup & Helpers

        // These are the dependencies we'll mock
        private readonly IPaymentRepository _mockRepository;
        private readonly IAppNotificationService _mockNotificationService;
        private readonly IPaymentGateway _mockPaystackGateway;
        private readonly ILogger<PaymentService> _mockLogger;
        private readonly PaymentService _sut; // SUT = System Under Test

        public PaymentServiceTests()
        {
            // Create mocks for all dependencies
            _mockRepository = Substitute.For<IPaymentRepository>();
            _mockNotificationService = Substitute.For<IAppNotificationService>();
            _mockPaystackGateway = Substitute.For<IPaymentGateway>();
            _mockLogger = Substitute.For<ILogger<PaymentService>>();

            // Configure the mock gateway to identify as Paystack
            _mockPaystackGateway.Provider.Returns(PaymentProvider.Paystack);

            // Create the service with mocked dependencies
            var gateways = new List<IPaymentGateway> { _mockPaystackGateway };
            _sut = new PaymentService(
                _mockRepository,
                _mockNotificationService,
                gateways,
                _mockLogger
            );
        }

        /// <summary>
        /// Helper to create a valid payment request
        /// </summary>
        private PaymentRequest CreateValidRequest(
            decimal amount = 1000m,
            PaymentProvider provider = PaymentProvider.Paystack)
        {
            return new PaymentRequest
            {
                ExternalUserId = "user@example.com",
                Amount = amount,
                Purpose = PaymentPurpose.ProductCheckout,
                Provider = provider,
                AppName = "TestApp",
                ExternalReference = "ORDER-123",
                RedirectUrl = "https://example.com/callback",
                NotificationUrl = "https://example.com/webhook"
            };
        }

        #endregion

        #region InitializePaymentAsync Tests

        [Fact]
        public async Task InitializePaymentAsync_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var request = CreateValidRequest();

            // Mock repository to save successfully
            _mockRepository.SaveChangesAsync()
                .Returns(Task.CompletedTask);

            // Mock gateway to return successful initialization
            var expectedAuthUrl = "https://checkout.paystack.com/xyz123";
            var gatewayResult = new PaymentInitResult("PB_test123", expectedAuthUrl);

            _mockPaystackGateway
                .InitializeAsync(Arg.Any<Payment>())
                .Returns(Result<PaymentInitResult>.Success(gatewayResult));

            // Act
            var result = await _sut.InitializePaymentAsync(request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AuthorizationUrl.Should().Be(expectedAuthUrl);

            // Verify repository was called
            await _mockRepository.Received(1).AddAsync(Arg.Any<Payment>());
            await _mockRepository.Received(1).SaveChangesAsync();

            // Verify gateway was called
            await _mockPaystackGateway.Received(1).InitializeAsync(Arg.Any<Payment>());
        }

        [Fact]
        public async Task InitializePaymentAsync_WithInvalidAmount_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest(amount: 0); // Invalid amount

            // Act
            var result = await _sut.InitializePaymentAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Amount must be greater than zero");
            result.ErrorCode.Should().Be("AMOUNT_NOT_POSITIVE");

            // Verify no repository calls were made
            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Payment>());
            await _mockRepository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task InitializePaymentAsync_WhenDatabaseSaveFails_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();

            // Mock repository to throw exception on save
            _mockRepository
                .When(x => x.SaveChangesAsync())
                .Do(x => throw new Exception("Database connection failed"));

            // Act
            var result = await _sut.InitializePaymentAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Database save failed");
            result.ErrorCode.Should().Be("DATABASE_ERROR");

            // Verify gateway was NOT called (save failed before gateway call)
            await _mockPaystackGateway.DidNotReceive().InitializeAsync(Arg.Any<Payment>());
        }

        [Fact]
        public async Task InitializePaymentAsync_WhenGatewayFails_ShouldMarkPaymentAsFailed()
        {
            // Arrange
            var request = CreateValidRequest();

            _mockRepository.SaveChangesAsync()
                .Returns(Task.CompletedTask);

            // Mock gateway to return failure
            _mockPaystackGateway
                .InitializeAsync(Arg.Any<Payment>())
                .Returns(Result<PaymentInitResult>.Failure(
                    "Gateway unavailable",
                    "GATEWAY_ERROR"));

            // Act
            var result = await _sut.InitializePaymentAsync(request);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Gateway unavailable");
            result.ErrorCode.Should().Be("GATEWAY_ERROR");

            // Verify repository was called twice (initial save + mark as failed)
            await _mockRepository.Received(2).SaveChangesAsync();
        }

        [Fact]
        public async Task InitializePaymentAsync_WhenExistingPendingPaymentExists_ShouldReinitialize()
        {
            // Arrange
            var request = CreateValidRequest();

            // Create an existing pending payment
            var existingPayment = new Payment(
                request.Provider,
                request.Purpose,
                Money.Create(request.Amount),
                Email.Create(request.ExternalUserId),
                request.AppName,
                request.ExternalReference,
                Url.Create(request.RedirectUrl),
                Url.Create(request.NotificationUrl)
            );

            // Mock repository to return existing payment
            _mockRepository
                .GetByExternalReferenceAsync(request.AppName, request.ExternalReference)
                .Returns(existingPayment);

            // Mock gateway for reinitialization
            var authUrl = "https://checkout.paystack.com/reinit123";
            _mockPaystackGateway
                .InitializeAsync(existingPayment)
                .Returns(Result<PaymentInitResult>.Success(
                    new PaymentInitResult(existingPayment.Reference.Value, authUrl)));

            // Act
            var result = await _sut.InitializePaymentAsync(request);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data!.Reference.Should().Be(existingPayment.Reference.Value);

            // Verify NO new payment was created
            await _mockRepository.DidNotReceive().AddAsync(Arg.Any<Payment>());

            // Verify gateway was called with existing payment
            await _mockPaystackGateway.Received(1).InitializeAsync(existingPayment);
        }

        #endregion

        #region HandleWebhookAsync Tests 

        [Fact]
        public async Task HandleWebhookAsync_WithValidSignatureAndData_ShouldProcessSuccessfully()
        {
            // Arrange
            var provider = "paystack";
            var jsonPayload = "{\"event\":\"charge.success\",\"data\":{\"reference\":\"PB_test123\",\"amount\":100000}}";
            var signature = "valid_signature";

            // Mock payment in database
            var paymentReference = PaymentReference.Generate();
            var referenceValue = paymentReference.Value;
            var payment = new Payment(
                PaymentProvider.Paystack,
                PaymentPurpose.ProductCheckout,
                Money.Create(1000m, "NGN"),
                Email.Create("user@example.com"),
                "TestApp",
                "ORDER-123",
                Url.Create("https://example.com/callback"),
                Url.Create("https://example.com/webhook")
            );

            _mockRepository
                .GetByReferenceAsync(paymentReference)
                .Returns(payment);

            // Mock gateway verification
            _mockPaystackGateway
                .VerifySignature(jsonPayload, signature)
                .Returns(Result<bool>.Success(true));

            _mockPaystackGateway
                .ParseWebhook(jsonPayload)
                .Returns(Result<PaymentVerificationResult>.Success(
                    new PaymentVerificationResult(referenceValue, 1000m)));

            _mockRepository
                .SaveChangesAsync()
                .Returns(Task.CompletedTask);

            _mockNotificationService
                .NotifyAppAsync(payment)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.HandleWebhookAsync(provider, jsonPayload, signature);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ResultType.Should().Be(WebhookResultType.Processed);

            // Verify notification was sent
            await _mockNotificationService.Received(1).NotifyAppAsync(payment);
        }

        [Fact]
        public async Task HandleWebhookAsync_WithInvalidSignature_ShouldFail()
        {
            // Arrange
            var provider = "paystack";
            var jsonPayload = "{\"event\":\"charge.success\"}";
            var invalidSignature = "wrong_signature";

            // Mock gateway to reject signature
            _mockPaystackGateway
                .VerifySignature(jsonPayload, invalidSignature)
                .Returns(Result<bool>.Failure("Invalid signature", "INVALID_SIGNATURE"));

            // Act
            var result = await _sut.HandleWebhookAsync(provider, jsonPayload, invalidSignature);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ResultType.Should().Be(WebhookResultType.Failed);
            result.Error.Should().Contain("Signature verification failed");

            // Verify no payment processing occurred
            await _mockRepository.DidNotReceive().GetByReferenceAsync(Arg.Any<PaymentReference>());
            await _mockNotificationService.DidNotReceive().NotifyAppAsync(Arg.Any<Payment>());
        }

        #endregion

        #region NSubstitute quick guide (in comments)

        /*
         * NSUBSTITUTE QUICK REFERENCE:
         * 
         * 1. CREATE A MOCK:
         *    var mock = Substitute.For<IInterface>();
         * 
         * 2. SETUP RETURN VALUES:
         *    mock.MethodName(arg).Returns(returnValue);
         *    mock.PropertyName.Returns(value);
         * 
         * 3. MATCH ANY ARGUMENT:
         *    mock.MethodName(Arg.Any<string>()).Returns(value);
         * 
         * 4. MATCH SPECIFIC ARGUMENT:
         *    mock.MethodName(Arg.Is<int>(x => x > 5)).Returns(value);
         * 
         * 5. VERIFY METHOD WAS CALLED:
         *    await mock.Received(1).MethodName(arg);
         *    await mock.Received().MethodName(Arg.Any<Type>());
         * 
         * 6. VERIFY METHOD WAS NOT CALLED:
         *    await mock.DidNotReceive().MethodName(arg);
         * 
         * 7. THROW EXCEPTION:
         *    mock.When(x => x.MethodName()).Do(x => throw new Exception());
         * 
         * 8. ASYNC METHODS:
         *    mock.MethodAsync().Returns(Task.FromResult(value));
         *    mock.MethodAsync().Returns(Task.CompletedTask);
         */

        #endregion

    }
}
