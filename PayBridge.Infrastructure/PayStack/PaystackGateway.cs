using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack
{
    public class PaystackGateway : IPaymentGateway
    {
        public PaymentProvider Provider => PaymentProvider.Paystack;

        public async Task<PaymentInitResult> InitializeAsync(Payment payment)
        {
            var amountInKobo = (int)(payment.Amount * 100);

            // This object represents the Paystack API Body
            var paystackPayload = new
            {
                email = payment.ExternalUserId,
                amount = amountInKobo,
                reference = payment.Reference,
                callback_url = payment.CallbackUrl, // Redirect user back to the app
                metadata = new
                {
                    internal_id = payment.Id, // CRITICAL: This links the webhook back to our DB
                    app_name = payment.AppName,
                    purpose = payment.Purpose.ToString()
                }
            };

            // TODO: Use HttpClient to call "https://api.paystack.co/transaction/initialize"
            // For now, return a dummy result
            return new PaymentInitResult(payment.Reference, "https://checkout.paystack.com/simulate-url");
        }

        public Task<PaymentVerificationResult> VerifyAsync(string reference)
        {
            throw new NotImplementedException();
        }
    }
}
