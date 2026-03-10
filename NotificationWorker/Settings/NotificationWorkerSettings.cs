namespace PayBridge.NotificationWorker.Settings
{
    public record NotificationWorkerSettings
    {
        /// <summary>
        /// How long to wait before the first retry attempt
        /// </summary>
        public int RetryIntervalSeconds { get; init; } = 30;

        /// <summary>
        /// Maximum number of retry attempts before the message moves to the dead-letter queue
        /// </summary>
        public int MaxRetryAttempts { get; init; } = 5;

        /// <summary>
        /// Timeout for each individual HTTP POST to the app's notification URL
        /// </summary>
        public int HttpTimeoutSeconds { get; init; } = 15;
    }
}