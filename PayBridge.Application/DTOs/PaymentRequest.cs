using PayBridge.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PayBridge.Application.DTOs
{
    public record PaymentRequest
    {
        
        public string ExternalUserId { get; init; } = default!;

        public decimal Amount { get; init; }
        
        public PaymentPurpose Purpose { get; init; }

        public PaymentProvider Provider { get; init; }

        public string AppName { get; init; } = default!;

        public string ExternalReference { get; init; } = default!;

        public string RedirectUrl { get; init; } = default!;

        public string NotificationUrl { get; init; } = default!;
    }
}
