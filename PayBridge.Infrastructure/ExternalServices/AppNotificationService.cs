using Microsoft.Extensions.Logging;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.ExternalServices
{
    public class AppNotificationService : IAppNotificationService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<AppNotificationService> logger;

        public AppNotificationService(IHttpClientFactory httpClient, ILogger<AppNotificationService> logger)
        {
            this.httpClient = httpClient.CreateClient();
            this.logger = logger;
        }

        public async Task NotifyAppAsync(Payment payment)
        {
            var payload = new
            {
                paymentReference = payment.Reference.Value,
                externalReference = payment.ExternalReference, // e.g orderId
                status = payment.Status.ToString(),
                amount = payment.Amount.Amount
            };

            // We use the RedirectUrl or a specific WebhookUrl stored in the DB
            // For a portfolio, sending it to the RedirectUrl's API endpoint is fine.
                logger.LogInformation(
                    "Sending payment notification. PaymentReference: {PaymentReference}, Url: {Url}",
                    payment.Reference,
                    payment.NotificationUrl
                );
            try
            {
                var response = await httpClient.PostAsJsonAsync(payment.NotificationUrl.Value, payload);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "Successfully notified app for PaymentReference: {PaymentReference}",
                        payment.Reference
                    );
                    return;
                }
                var errorBody = await response.Content.ReadAsStringAsync();

                logger.LogWarning(
                    "Failed to notify app for PaymentReference: {PaymentReference}. StatusCode: {StatusCode}. Response: {Response}",
                    payment.Reference, (int)response.StatusCode, errorBody
                );
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(
                    ex,
                    "Timeout while notifying app for PaymentReference: {PaymentReference}. Url: {Url}",
                    payment.Reference,
                    payment.NotificationUrl
                );
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "Unexpected error while notifying app for PaymentReference: {PaymentReference}",
                    payment.Reference
                );
            }
        }
    }
}
