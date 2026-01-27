using FluentValidation;
using PayBridge.Application.DTOs;
using PayBridge.Domain.Enums;

namespace PayBridge.Application.Validators
{
    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.ExternalUserId)
                .NotEmpty()
                .WithMessage("ExternalUserId is required")
                .EmailAddress()
                .WithMessage("ExternalUserId must be a valid email address")
                .MaximumLength(254)
                .WithMessage("Email address cannot exceed 254 characters");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero")
                .LessThanOrEqualTo(100_000_000)
                .WithMessage("Amount cannot exceed 100,000,000")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .WithMessage("Amount can have at most 2 decimal places");

            RuleFor(x => x.Purpose)
                .IsInEnum()
                .WithMessage("Purpose must be a valid payment purpose");

            RuleFor(x => x.Provider)
                .IsInEnum()
                .WithMessage("Provider must be a valid payment provider");

            RuleFor(x => x.AppName)
                .NotEmpty()
                .WithMessage("AppName is required")
                .Length(1, 100)
                .WithMessage("AppName must be between 1 and 100 characters")
                .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
                .WithMessage("AppName can only contain letters, numbers, spaces, hyphens, underscores, and periods");

            RuleFor(x => x.ExternalReference)
                .NotEmpty()
                .WithMessage("ExternalReference is required")
                .Length(1, 200)
                .WithMessage("ExternalReference must be between 1 and 200 characters");

            RuleFor(x => x.RedirectUrl)
                .NotEmpty()
                .WithMessage("RedirectUrl is required")
                .Must(BeAValidUrl)
                .WithMessage("RedirectUrl must be a valid HTTP or HTTPS URL")
                .MaximumLength(500)
                .WithMessage("RedirectUrl cannot exceed 500 characters");

            RuleFor(x => x.NotificationUrl)
                .NotEmpty()
                .WithMessage("NotificationUrl is required")
                .Must(BeAValidUrl)
                .WithMessage("NotificationUrl must be a valid HTTP or HTTPS URL")
                .MaximumLength(500)
                .WithMessage("NotificationUrl cannot exceed 500 characters");
        }

        /// <summary>
        /// Custom validation for URLs
        /// Ensures URL is well-formed and uses HTTP/HTTPS scheme
        /// </summary>
        private bool BeAValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}