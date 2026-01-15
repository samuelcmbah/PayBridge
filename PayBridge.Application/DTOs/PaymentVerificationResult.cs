using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public class PaymentVerificationResult
    {
        public string Reference { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public decimal Amount { get; set; }
    }
}
