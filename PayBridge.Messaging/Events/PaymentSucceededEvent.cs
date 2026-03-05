namespace PayBridge.Messaging.Events
{
    /// <summary>
    /// Published by PayBridge when a payment is successfully verified via webhook.
    /// Consumed by PayBridge.NotificationWorker to notify the originating app.
    ///
    /// IMPORTANT: Never rename or remove properties from this class without
    /// a versioning strategy. Both publisher and consumer must agree on the shape.
    /// In a company setting, this would be a versioned NuGet package.
    /// </summary>
    public record PaymentSucceededEvent
    {        
        public string PaymentReference { get; init; } = default!;
        
        public string ExternalReference { get; init; } = default!;
        
        public string AppName { get; init; } = default!;
      
        public decimal Amount { get; init; }

        public string Currency { get; init; } = default!;

        public string NotificationUrl { get; init; } = default!;

        public DateTime OccurredAt { get; init; }
    }
}