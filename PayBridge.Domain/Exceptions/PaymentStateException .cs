using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.Exceptions
{
    public sealed class PaymentStateException(string message, string errorCode = "INVALID_PAYMENT_STATE") : DomainException(message, errorCode)
    {
    }
}
