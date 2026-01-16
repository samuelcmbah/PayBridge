using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
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
        private readonly IEnumerable<IPaymentGateway> gateways;

        public PaymentService(IPaymentRepository paymentRepository, IEnumerable<IPaymentGateway> gateways)
        {
            this.paymentRepository = paymentRepository;
            this.gateways = gateways;
        }
        public Task HandleWebhookAsync(WebhookPayload payload)
        {
            throw new NotImplementedException();
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
                request.ExternalReference, request.CallbackUrl
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
