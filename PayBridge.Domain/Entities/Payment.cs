using PayBridge.Domain.Enums;
using PayBridge.Domain.Exceptions;
using PayBridge.Domain.ValueObjects;

namespace PayBridge.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public PaymentReference Reference { get; private set; } = default!;
    public PaymentProvider Provider { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentPurpose Purpose { get; private set; }
    public Money Amount { get; private set; }

    public Email ExternalUserId { get; private set; } = default!; 
    public string AppName { get; private set; } = default!;
    public string ExternalReference { get; private set; } = default!; // e.g., OrderId
    public Url RedirectUrl { get; private set; } = default!;
    public Url NotificationUrl { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    private Payment() { } // ef core needs a parameterless constructor when querying database. it is private so that app code cant use it

    public Payment(
            PaymentProvider provider,
            PaymentPurpose purpose,
            Money amount,
            Email externalUserId,
            string appName,
            string externalReference,
            Url redirectUrl,
            Url notificationUrl)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new PaymentStateException("AppName cannot be empty", "INVALID_APP_NAME");

        if (string.IsNullOrWhiteSpace(externalReference))
            throw new PaymentStateException("ExternalReference cannot be empty", "INVALID_EXTERNAL_REFERENCE");

        Id = Guid.NewGuid();
        Reference = PaymentReference.Generate();
        Provider = provider;
        Purpose = purpose;
        Amount = amount;
        ExternalUserId = externalUserId;
        AppName = appName.Trim();
        ExternalReference = externalReference.Trim();
        RedirectUrl = redirectUrl;
        NotificationUrl = notificationUrl;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public PaymentProcessingResult ProcessSuccessfulPayment(Money receivedAmount)
    {
        if (Status != PaymentStatus.Pending)
            throw new PaymentStateException(
                $"Payment already processed with status: {Status}",
                "ALREADY_PROCESSED");

        if (!receivedAmount.Equals(Amount))
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

