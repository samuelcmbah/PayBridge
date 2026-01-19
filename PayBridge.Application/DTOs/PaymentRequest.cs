using PayBridge.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PayBridge.Application.DTOs
{
    public record PaymentRequest
    {
        [Required(ErrorMessage = "ExternalUserId is required")]
        [EmailAddress(ErrorMessage = "ExternalUserId must be a valid email")]
        public string ExternalUserId { get; init; } = default!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; init; }

        [Required]
        public PaymentPurpose Purpose { get; init; }

        [Required]
        public PaymentProvider Provider { get; init; }

        [Required(ErrorMessage = "AppName is required")]
        [StringLength(100, MinimumLength = 1)]
        public string AppName { get; init; } = default!;

        [Required(ErrorMessage = "ExternalReference is required")]
        [StringLength(200, MinimumLength = 1)]
        public string ExternalReference { get; init; } = default!;

        [Required(ErrorMessage = "RedirectUrl is required")]
        [Url(ErrorMessage = "RedirectUrl must be a valid URL")]
        public string RedirectUrl { get; init; } = default!;

        [Required(ErrorMessage = "NotificationUrl is required")]
        [Url(ErrorMessage = "NotificationUrl must be a valid URL")]
        public string NotificationUrl { get; init; } = default!;
    }
}
