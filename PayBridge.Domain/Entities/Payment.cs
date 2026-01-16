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
        public string Reference { get; private set; } // Our internal Ref: PB_123...
        public PaymentProvider Provider { get; private set; }
        public PaymentStatus Status { get; private set; }
        public PaymentPurpose Purpose { get; private set; }

        public decimal Amount { get; private set; }
        public string Currency { get; private set; } // e.g., "NGN"
        public string ExternalUserId { get; private set; } // User's email

        // Tracking for the calling apps (SportStore/ExpenseVista)
        public string AppName { get; private set; }
        public string ExternalReference { get; private set; } // e.g., OrderId
        public string CallbackUrl { get; private set; } // Where user goes after UI payment

        public DateTime CreatedAt { get; private set; }
        public DateTime? VerifiedAt { get; private set; }

        private Payment() { } // For EF Core

        public Payment(string reference, PaymentProvider provider, PaymentPurpose purpose,
                       decimal amount, string externalUserId, string appName,
                       string externalReference, string callbackUrl)
        {
            Id = Guid.NewGuid();
            Reference = reference;
            Provider = provider;
            Purpose = purpose;
            Amount = amount;
            Currency = "NGN";
            ExternalUserId = externalUserId;
            AppName = appName;
            ExternalReference = externalReference;
            CallbackUrl = callbackUrl;
            Status = PaymentStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkSuccessful()
        {
            Status = PaymentStatus.Success;
            VerifiedAt = DateTime.UtcNow;
        }

        public void MarkFailed()
        {
            Status = PaymentStatus.Failed;
            VerifiedAt = DateTime.UtcNow;
        }
    }
}
