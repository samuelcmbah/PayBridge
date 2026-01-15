using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public class PaymentInitResult
    {
        public string Reference { get; set; } = string.Empty;
        public string AuthorizationUrl { get; set; } = string.Empty;
    }
}
