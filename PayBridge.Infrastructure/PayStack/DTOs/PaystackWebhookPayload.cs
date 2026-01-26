using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack.DTOs
{
    internal class PaystackWebhookPayload
    {
        public string Event { get; set; } = default!;
        public PaystackWebhookData Data { get; set; } = new PaystackWebhookData();
    }
}
