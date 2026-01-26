using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack.DTOs
{
    public class PaystackInitResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = default!;
        public PaystackInitData Data { get; set; } = new PaystackInitData();
    }
}
