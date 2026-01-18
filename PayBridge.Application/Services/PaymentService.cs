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
            logger.LogInformation(
                "Initializing payment - AppName: {AppName}, Amount: {Amount}, Provider: {Provider}, ExternalRef: {ExternalReference}",
                request.AppName, request.Amount, request.Provider, request.ExternalReference
            );


            // Check for existing pending payment (Idempotency)
            var existingPayment = await paymentRepository.GetByExternalReferenceAsync(request.AppName, request.ExternalReference);

            if (existingPayment != null && existingPayment.Status == PaymentStatus.Pending)
            {
                logger.LogInformation(
                    "Found existing pending payment with reference {Reference} for ExternalRef: {ExternalReference}",
                    existingPayment.Reference, request.ExternalReference
                );
                var existingGateway = gateways.FirstOrDefault(g => g.Provider == existingPayment.Provider);

                if(existingGateway == null)
                {
                    return Result<PaymentInitResult>.Failure(
                        "Payment gateway for existing payment is no longer available",
                        "GATEWAY_UNAVAILABLE"
                    );
                }
                try
                {
                    var existingResult = await existingGateway.InitializeAsync(existingPayment);
                    return Result<PaymentInitResult>.Success(existingResult);
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
                logger.LogWarning(
                    "Payment provider '{Provider}' is not supported. Available providers: {AvailableProviders}",
                    request.Provider,
                    string.Join(", ", gateways.Select(g => g.Provider))
                );

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
            try
            {
                await paymentRepository.AddAsync(payment);
                await paymentRepository.SaveChangesAsync();

                logger.LogInformation(
                    "Payment record created successfully - Reference: {Reference}",
                    payment.Reference
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to save payment to database - Reference: {Reference}, ExternalRef: {ExternalReference}",
                    reference, request.ExternalReference
                );

                return Result<PaymentInitResult>.Failure(
                    "Failed to create payment record. Please try again.",
                    "DATABASE_ERROR"
                );
            }

            // Call Gateway to initialize payment
            try
            {
                var result = await gateway.InitializeAsync(payment);

                logger.LogInformation(
                    "Payment initialized successfully with {Provider} - Reference: {Reference}, AuthUrl: {AuthUrl}",
                    request.Provider, payment.Reference, result.AuthorizationUrl
                );

                return Result<PaymentInitResult>.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to initialize payment with {Provider} - Reference: {Reference}",
                    request.Provider, payment.Reference
                );

                // Try to mark payment as failed
                try
                {
                    payment.MarkFailed();
                    await paymentRepository.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    logger.LogError(
                        saveEx,
                        "Failed to mark payment as failed in database - Reference: {Reference}",
                        payment.Reference
                    );
                }

                return Result<PaymentInitResult>.Failure(
                    $"Failed to initialize payment with {request.Provider}. Please try again.",
                    "GATEWAY_INITIALIZATION_ERROR"
                );
            }
        }

        public async Task HandleWebhookAsync(string provider, string jsonPayload, string signature)
        {
            var gateway = gateways.First(g => g.Provider.ToString().Equals(provider, StringComparison.OrdinalIgnoreCase));

            var requestVerified = await gateway.VerifySignatureAsync(jsonPayload, signature);
            if (!requestVerified)
            {
                throw new Exception("Invalid webhook signature");
            }

            // Parse the payload to get our reference
            var verification = await gateway.ParseWebhookAsync(jsonPayload);

            var payment = await paymentRepository.GetByReferenceAsync(verification.Reference);
            if (payment == null || payment.Status != PaymentStatus.Pending)
            {
                return;
            }
            //Double check amount(Security: ensure they paid what we asked)
            if (verification.Amount != payment.Amount)
            {
                payment.MarkFailed();
            }
            else
            {
                payment.MarkSuccessful();
            }

            await paymentRepository.SaveChangesAsync();

            await appNotificationService.NotifyAppAsync(payment);
        }

        
    }
}
