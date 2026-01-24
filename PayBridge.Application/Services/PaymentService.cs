using Microsoft.Extensions.Logging;
using PayBridge.Application.Common;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository paymentRepository;
        private readonly IAppNotificationService appNotificationService;
        private readonly IEnumerable<IPaymentGateway> gateways;
        private readonly ILogger<PaymentService> logger;

        public PaymentService(IPaymentRepository paymentRepository,
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
            var existingPayment = await paymentRepository.GetByExternalReferenceAsync(request.AppName, request.ExternalReference);

            if (existingPayment != null && existingPayment.Status == PaymentStatus.Pending)
            {
                var existingGateway = gateways.FirstOrDefault(g => g.Provider == existingPayment.Provider);

                if (existingGateway == null)
                {
                    return Result<PaymentInitResult>.Failure(
                        "Payment gateway for existing payment is no longer available",
                        "GATEWAY_UNAVAILABLE"
                    );
                }
                try
                {
                    var existingResult = await existingGateway.InitializeAsync(existingPayment);
                    return existingResult;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to reinitialize existing payment {Reference}", existingPayment.Reference);
                    return Result<PaymentInitResult>.Failure(
                        "Failed to retrieve existing payment details",
                        "EXISTING_PAYMENT_ERROR"
                    );
                }
            }
            //Resolve the correct Gateway
            var gateway = gateways.FirstOrDefault(g => g.Provider == request.Provider);

            if (gateway == null)
            {

                return Result<PaymentInitResult>.Failure(
                    $"Payment provider '{request.Provider}' is not supported",
                    "UNSUPPORTED_PROVIDER"
                );
            }

            // create domain entity
            var payment = new Payment(
                request.Provider,
                request.Purpose,
                request.Amount,
                request.ExternalUserId,
                request.AppName,
                request.ExternalReference,
                request.RedirectUrl,
                request.NotificationUrl
            );

            // Persist to DB first
            var saveResult = await SavePaymentAsync(payment);
            if (!saveResult.IsSuccess)
            {
                return Result<PaymentInitResult>.Failure(saveResult.Error, "DATABASE_ERROR");
            }


            // Call Gateway to initialize payment
            var gatewayResult = await gateway.InitializeAsync(payment);

            if (!gatewayResult.IsSuccess)
            {
                payment.MarkInitializationFailed();

                try
                {
                    await paymentRepository.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to mark payment {Reference} as failed after gateway error",
                        payment.Reference);
                }

                return Result<PaymentInitResult>.Failure(
                    gatewayResult.Error,
                    "GATEWAY_ERROR"
                );
            }

            return gatewayResult;
        }

        public async Task<WebhookResult> HandleWebhookAsync(string provider, string jsonPayload, string signature)
        {
            var gateway = gateways.FirstOrDefault(g =>
                g.Provider.ToString().Equals(provider, StringComparison.OrdinalIgnoreCase));

            if (gateway == null)
            {
                return WebhookResult.Failed($"Unsupported provider: {provider}");
            }

            var signatureResult = gateway.VerifySignature(jsonPayload, signature);
            if (!signatureResult.IsSuccess)
            {
                return WebhookResult.Failed($"Signature verification failed: {signatureResult.Error}");
            }

            // Parse the payload to get our reference
            var parseResult = gateway.ParseWebhook(jsonPayload);
            if (!parseResult.IsSuccess)
            {
                // Some parse failures are expected (e.g., unsupported event types)
                if (parseResult.ErrorCode == "UNSUPPORTED_EVENT")
                {
                    return WebhookResult.Ignored($"Event type not processed: {parseResult.Error}");
                }

                return WebhookResult.Failed($"Failed to parse webhook: {parseResult.Error}");
            }

            var verification = parseResult.Data!;

            //Find the payment
            var payment = await paymentRepository.GetByReferenceAsync(verification.Reference);
            if (payment == null)
            {
                return WebhookResult.Ignored($"Payment not found: {verification.Reference}");
            }
            var result = payment.ProcessSuccessfulPayment(verification.Amount);

            try
            {
                await paymentRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return WebhookResult.Failed($"Failed to save payment status: {ex.Message}");
            }

            //Notify the calling application
            if (result == PaymentProcessingResult.Success)
            {
                try
                {
                    await appNotificationService.NotifyAppAsync(payment);
                    return WebhookResult.Success();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to notify app for payment {Reference}", payment.Reference);
                    // The notification failure should be handled separately (retry queue, etc.)
                    // For now, we swallow intentionally. Payment is aready successful
                }
            }

            return result switch
            {
                PaymentProcessingResult.Success => WebhookResult.Success(),
                PaymentProcessingResult.AmountMismatch => WebhookResult.Failed("Amount mismatch"),
                _ => WebhookResult.Ignored("Already processed")

            };

        }


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
                logger.LogError(ex, "Failed to save payment {Reference}", payment.Reference);
                return Result<bool>.Failure("Database save failed");
            }
        }

       
    }
}
