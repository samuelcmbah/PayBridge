using PayBridge.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.IServices
{
    public interface IPaymentService
    {
        Task<PaymentInitResult> InitializePaymentAsync(PaymentRequest request);

        Task HandleWebhookAsync(WebhookPayload payload);
    }
}
