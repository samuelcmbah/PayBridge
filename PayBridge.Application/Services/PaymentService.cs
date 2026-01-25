using Microsoft.Extensions.Logging;
using PayBridge.Application.Common;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using PayBridge.Domain.Exceptions;
using PayBridge.Domain.ValueObjects;
using System;

namespace PayBridge.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository paymentRepository;
        private readonly IAppNotificationService appNotificationService;
        private readonly IEnumerable<IPaymentGateway> gateways;
        private readonly ILogger<PaymentService> logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IAppNotificationService appNotificationService,
            IEnumerable<IPaymentGateway> gateways,
            ILogger<PaymentService> logger)
        {
            this.paymentRepository = paymentRepository;
            this.appNotificationService = appNotificationService;
            this.gateways = gateways;
            this.logger = logger;
        }

        public async Task<Result<PaymentInitResult>> InitializePaymentAsync(PaymentRequest request)
        {
            // Check for existing pending payment (Idempotency)
            var existingPayment = await paymentRepository.GetByExternalReferenceAsync(
                request.AppName,
                request.ExternalReference);

            if (existingPayment != null && existingPayment.Status == PaymentStatus.Pending)
            {
                return await ReinitializeExistingPaymentAsync(existingPayment);
            }

            
            try
            {
                // Create value objects(may throw domain exceptions)
                var amount = Money.Create(request.Amount);
                var email = Email.Create(request.ExternalUserId);
                var redirectUrl = Url.Create(request.RedirectUrl);
                var notificationUrl = Url.Create(request.NotificationUrl);

                // Create payment entity(may throw domain exceptions)
                var payment = new Payment(
                    request.Provider,
                    request.Purpose,
                    amount,
                    email,
                    request.AppName,
                    request.ExternalReference,
                    redirectUrl,
                    notificationUrl);

                var saveResult = await SavePaymentAsync(payment);
                if (!saveResult.IsSuccess)
                {
                    return Result<PaymentInitResult>.Failure(
                        saveResult.Error,
                        "DATABASE_ERROR");
                }

                var gateway = ResolveGateway(request.Provider);
                if (gateway == null)
                {
                    return Result<PaymentInitResult>.Failure(
                        $"Payment provider '{request.Provider}' is not supported",
                        "UNSUPPORTED_PROVIDER");
                }

                var gatewayResult = await gateway.InitializeAsync(payment);
                if (!gatewayResult.IsSuccess)
                {
                    await TryMarkPaymentAsFailedAsync(payment);

                    return Result<PaymentInitResult>.Failure(
                        gatewayResult.Error,
                        "GATEWAY_ERROR");
                }

                return gatewayResult;
            }
            catch (DomainException ex)
            {
                logger.LogWarning(ex, "Domain validation failed");
                return Result<PaymentInitResult>.Failure(ex.Message, ex.ErrorCode);
            }
           
        }

        public async Task<WebhookResult> HandleWebhookAsync(string provider, string jsonPayload, string signature)
        {
            try
            {
                var gateway = ResolveGateway(provider);
                if (gateway == null)
                {
                    return WebhookResult.Failed($"Unsupported provider: {provider}");
                }

                var signatureResult = gateway.VerifySignature(jsonPayload, signature);
                if (!signatureResult.IsSuccess)
                {
                    return WebhookResult.Failed(
                        $"Signature verification failed: {signatureResult.Error}");
                }

                var parseResult = gateway.ParseWebhook(jsonPayload);
                if (!parseResult.IsSuccess)
                {
                    return parseResult.ErrorCode == "UNSUPPORTED_EVENT"
                        ? WebhookResult.Ignored($"Event type not processed: {parseResult.Error}")
                        : WebhookResult.Failed($"Failed to parse webhook: {parseResult.Error}");
                }

                var verification = parseResult.Data!;

                var receivedReference = PaymentReference.Create(verification.Reference);

                var payment = await paymentRepository.GetByReferenceAsync(receivedReference);
                if (payment == null)
                {
                    return WebhookResult.Ignored($"Payment not found: {verification.Reference}");
                }

                // Create Money value object
                var receivedAmount = Money.Create(verification.Amount, payment.Amount.Currency);


                var processResult = payment.ProcessSuccessfulPayment(receivedAmount);

                var saveResult = await TrySaveChangesAsync();
                if (!saveResult.IsSuccess)
                {
                    return WebhookResult.Failed($"Failed to save payment status: {saveResult.Error}");
                }

                if (processResult == PaymentProcessingResult.Success)
                {
                    await TryNotifyAppAsync(payment);
                }

                // Map result to webhook response
                return processResult switch
                {
                    PaymentProcessingResult.Success => WebhookResult.Success(),
                    PaymentProcessingResult.AmountMismatch => WebhookResult.Failed("Amount mismatch"),
                    _ => WebhookResult.Ignored("Already processed")
                };
            }
            catch (DomainException ex)
            {
                logger.LogError(ex, "Domain error processing webhook");
                return WebhookResult.Failed(ex.Message);
            }
            
        }

        #region Private Helper Methods

        /// <summary>
        /// Resolves the payment gateway for the given provider
        /// </summary>
        private IPaymentGateway? ResolveGateway(PaymentProvider provider)
        {
            return gateways.FirstOrDefault(g => g.Provider == provider);
        }

        /// <summary>
        /// Resolves the payment gateway for the given provider name
        /// </summary>
        private IPaymentGateway? ResolveGateway(string providerName)
        {
            return gateways.FirstOrDefault(g =>
                g.Provider.ToString().Equals(providerName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Attempts to reinitialize an existing pending payment
        /// </summary>
        private async Task<Result<PaymentInitResult>> ReinitializeExistingPaymentAsync(Payment payment)
        {
            var gateway = ResolveGateway(payment.Provider);
            if (gateway == null)
            {
                return Result<PaymentInitResult>.Failure(
                    "Payment gateway for existing payment is no longer available",
                    "GATEWAY_UNAVAILABLE");
            }

           
            var result = await gateway.InitializeAsync(payment);

            if (!result.IsSuccess)
            {
                logger.LogError(
                    "Failed to reinitialize existing payment {Reference}: {Error}",
                    payment.Reference,
                    result.Error);
            }

            return result;
        }

        /// <summary>
        /// Saves a payment to the database with error handling
        /// </summary>
        private async Task<Result<bool>> SavePaymentAsync(Payment payment)
        {
            try
            {
                await paymentRepository.AddAsync(payment);
                await paymentRepository.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to save payment {Reference}",
                    payment.Reference);
                return Result<bool>.Failure("Database save failed");
            }
        }

        /// <summary>
        /// Attempts to save database changes with error handling
        /// </summary>
        private async Task<Result<bool>> TrySaveChangesAsync()
        {
            try
            {
                await paymentRepository.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save changes to database");
                return Result<bool>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Attempts to mark a payment as failed (fire-and-forget)
        /// Best effort operation - failure is logged but not propagated
        /// </summary>
        private async Task TryMarkPaymentAsFailedAsync(Payment payment)
        {
            try
            {
                payment.MarkInitializationFailed();
                await paymentRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to mark payment {Reference} as failed after gateway error",
                    payment.Reference);
                // Swallow exception - this is a best-effort operation
            }
        }

        /// <summary>
        /// Attempts to notify the app about payment completion (fire-and-forget)
        /// Best effort operation - failure is logged but not propagated
        /// </summary>
        private async Task TryNotifyAppAsync(Payment payment)
        {
            try
            {
                await appNotificationService.NotifyAppAsync(payment);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to notify app for payment {Reference}",
                    payment.Reference);
                // Swallow exception - payment is already marked successful
                // Notification failures should be handled via retry queue
            }
        }

        #endregion
    }
}