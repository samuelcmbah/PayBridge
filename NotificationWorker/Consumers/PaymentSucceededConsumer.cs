using MassTransit;
using Microsoft.Extensions.Options;
using PayBridge.Messaging.Events;
using PayBridge.NotificationWorker.Settings;
using System.Net.Http.Json;

namespace PayBridge.NotificationWorker.Consumers
{
    /// <summary>
    /// Consumes PaymentSucceededEvent messages from RabbitMQ and delivers
    /// HTTP notifications to the originating app's notification endpoint.
    ///
    /// Retry and dead-letter behaviour is configured in Program.cs.
    /// MassTransit will automatically retry this consumer if it throws an exception,
    /// using the retry policy defined on the receive endpoint.
    /// </summary>
    public class PaymentSucceededConsumer : IConsumer<PaymentSucceededEvent>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentSucceededConsumer> _logger;
        private readonly NotificationWorkerSettings _settings;

        public PaymentSucceededConsumer(
            HttpClient httpClient,
            IOptions<NotificationWorkerSettings> settings,
            ILogger<PaymentSucceededConsumer> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
        {
            var @event = context.Message;

            _logger.LogInformation(
                "Processing PaymentSucceededEvent - PaymentReference: {PaymentReference}, " +
                "App: {AppName}, NotificationUrl: {NotificationUrl}",
                @event.PaymentReference,
                @event.AppName,
                @event.NotificationUrl);

            var payload = BuildNotificationPayload(@event);

            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(_settings.HttpTimeoutSeconds));

            var response = await _httpClient.PostAsJsonAsync(
                @event.NotificationUrl,
                payload,
                cts.Token);

            // If the app returns a non-success status code, we treat it as a failure
            // and allow MassTransit's retry policy to kick in.
            // This means the message stays in the queue and is retried.
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Notification delivery failed - PaymentReference: {PaymentReference}, " +
                    "App: {AppName}, StatusCode: {StatusCode}, Response: {Response}",
                    @event.PaymentReference,
                    @event.AppName,
                    (int)response.StatusCode,
                    responseBody);

                // Throwing causes MassTransit to retry according to the configured policy.
                // After all retries are exhausted, the message moves to the dead-letter queue.
                throw new NotificationDeliveryException(
                    $"App returned {(int)response.StatusCode} for PaymentReference: {@event.PaymentReference}");
            }

            _logger.LogInformation(
                "Notification delivered successfully - PaymentReference: {PaymentReference}, App: {AppName}",
                @event.PaymentReference,
                @event.AppName);
        }

        /// <summary>
        /// Builds the payload that gets POSTed to the app's notification endpoint.
        /// This is what SportStore's notification endpoint will receive.
        /// </summary>
        private static object BuildNotificationPayload(PaymentSucceededEvent @event)
        {
            return new
            {
                paymentReference = @event.PaymentReference,
                externalReference = @event.ExternalReference,   // e.g. ORDER-123
                status = "Success",
                amount = @event.Amount,
                currency = @event.Currency,
                occurredAt = @event.OccurredAt
            };
        }
    }

    /// <summary>
    /// Thrown when the downstream app returns a non-success HTTP response.
    /// Signals MassTransit to retry the message according to the configured policy.
    /// </summary>
    public class NotificationDeliveryException : Exception
    {
        public NotificationDeliveryException(string message) : base(message) { }
    }
}