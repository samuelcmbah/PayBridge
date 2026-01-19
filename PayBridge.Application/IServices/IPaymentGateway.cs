using PayBridge.Application.Common;
using PayBridge.Application.DTOs;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.IServices
{
    public interface IPaymentGateway
    {
        PaymentProvider Provider { get; }

        Task<Result<PaymentInitResult>> InitializeAsync(Payment request);

        /// <summary>
        /// Verifies that the webhook request came from the payment provider
        /// </summary>
        Result<bool> VerifySignature(string jsonPayload, string signature);

        /// <summary>
        /// Parses the webhook payload to extract payment information
        /// </summary>
        Result<PaymentVerificationResult> ParseWebhook(string jsonPayload);
    }
}
