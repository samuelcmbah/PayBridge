using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; private set; }
        public string Reference { get; private set; } = default!;
        public PaymentProvider Provider { get; private set; }
        public PaymentStatus Status { get; private set; }
        public PaymentPurpose Purpose { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = default!;
        public string ExternalUserId { get; private set; } = default!; // User's email
        public string AppName { get; private set; } = default!;
        public string ExternalReference { get; private set; } = default!; // e.g., OrderId
        public string RedirectUrl { get; private set; } = default!; 
        public string NotificationUrl { get; private set; } = default!; 
        public DateTime CreatedAt { get; private set; }
        public DateTime? VerifiedAt { get; private set; }

        private Payment() { } // ef core needs a parameterless constructor when querying database. it is private so that app code cant use it

        public Payment(PaymentProvider provider, PaymentPurpose purpose,
                       decimal amount, string externalUserId, string appName,
                       string externalReference, string redirectUrl, string notificationUrl)
        {
            Id = Guid.NewGuid();
            Reference = $"PB_{Guid.NewGuid():N}";
            Provider = provider;
            Purpose = purpose;
            Amount = amount;
            Currency = "NGN";
            ExternalUserId = externalUserId;
            AppName = appName;
            ExternalReference = externalReference;
            RedirectUrl = redirectUrl;
            NotificationUrl = notificationUrl;
            Status = PaymentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public PaymentProcessingResult ProcessSuccessfulPayment(decimal receivedAmount)
        {
            if (Status != PaymentStatus.Pending)
                return PaymentProcessingResult.AlreadyProcessed;

            if (receivedAmount != Amount)
            {
                MarkFailed();
                return PaymentProcessingResult.AmountMismatch;
            }

            MarkSuccessful();
            return PaymentProcessingResult.Success;
        }

        public void MarkInitializationFailed()
        {
            if (Status != PaymentStatus.Pending)
                return;

            MarkFailed();
        }


        private void MarkSuccessful()
        {
            Status = PaymentStatus.Success;
            VerifiedAt = DateTime.UtcNow;
        }

        private void MarkFailed()
        {
            Status = PaymentStatus.Failed;
            VerifiedAt = DateTime.UtcNow;
        }
    }
}
