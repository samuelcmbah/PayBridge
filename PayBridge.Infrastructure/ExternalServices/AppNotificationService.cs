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

        public AppNotificationService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
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
            await httpClient.PostAsJsonAsync(payment.CallbackUrl, payload);
        }
    }
}
