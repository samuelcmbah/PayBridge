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

        public AppNotificationService(IHttpClientFactory httpClient)
        {
            this.httpClient = httpClient.CreateClient();
        }

        public async Task NotifyAppAsync(Payment payment)
        {
            var payload = new
            {
                paymentReference = payment.Reference,
                externalReference = payment.ExternalReference, // e.g orderId
                status = payment.Status.ToString(),
                amount = payment.Amount
            };

            // We use the RedirectUrl or a specific WebhookUrl stored in the DB
            // For a portfolio, sending it to the RedirectUrl's API endpoint is fine.
            try
            {
                var response = await httpClient.PostAsJsonAsync(payment.NotificationUrl, payload);
                Console.WriteLine($"🔔 Notifying app at: {payment.NotificationUrl}");
                Console.WriteLine($"📦 Payload: {System.Text.Json.JsonSerializer.Serialize(payload)}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    // Log the error for debugging
                    Console.WriteLine($"Failed to notify app: {error}");
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - webhook notification is best-effort
                Console.WriteLine($"Error notifying app: {ex.Message}");
            }
        }
    }
}
