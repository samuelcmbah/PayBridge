using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public class PaymentRequest
    {
        public string ExternalUserId { get; set; } = string.Empty; // UserId from SportStore / ExpenseVista
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "NGN";
        public PaymentPurpose Purpose { get; set; }
        public PaymentProvider Provider { get; set; }
        public string CallbackUrl { get; set; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
