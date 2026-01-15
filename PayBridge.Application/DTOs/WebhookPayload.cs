using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public class WebhookPayload
    {
        public string Provider { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
