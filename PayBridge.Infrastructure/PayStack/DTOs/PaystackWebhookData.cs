using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack.DTOs
{
    internal class PaystackWebhookData
    {
        public string Reference { get; set; } = default!;
        public int Amount { get; set; } // Paystack sends amount in Kobo (int)
        public string Status { get; set; } = default!;
        public string Currency { get; set; } = default!;
    }
}
