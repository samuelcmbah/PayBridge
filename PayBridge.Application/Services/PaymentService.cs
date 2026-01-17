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

        public PaymentService(IPaymentRepository paymentRepository,
            IAppNotificationService appNotificationService,
            IEnumerable<IPaymentGateway> gateways)
        {
            this.paymentRepository = paymentRepository;
            this.appNotificationService = appNotificationService;
            this.gateways = gateways;
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

        public async Task<PaymentInitResult> InitializePaymentAsync(PaymentRequest request)
        {
            // Generate internal reference
            var reference = $"PB_{Guid.NewGuid():N}";

            // Create Domain Entity
            var payment = new Payment(
                reference, request.Provider, 
                request.Purpose, request.Amount,
                request.ExternalUserId, request.AppName, 
                request.ExternalReference, request.RedirectUrl,
                request.NotificationUrl
            );

            // Persist to DB first (Ensures we have a record before the user leaves our site)
            await paymentRepository.AddAsync(payment);
            await paymentRepository.SaveChangesAsync();

            // Resolve the correct Gateway (Paystack/Flutterwave)
            var gateway = gateways.FirstOrDefault(g => g.Provider == request.Provider) 
                            ?? throw new Exception("Provider not supported");

            try
            {
                // 5. Call Gateway
                return await gateway.InitializeAsync(payment);
            }
            catch
            {
                // Fail gracefully if Provider is down
                payment.MarkFailed();
                await paymentRepository.SaveChangesAsync();
                throw;
            }
        }
    }
}
