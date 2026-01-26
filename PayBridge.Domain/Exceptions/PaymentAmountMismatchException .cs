using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.Exceptions
{
    public sealed class PaymentAmountMismatchException(string message = "Payment amount does not match expected amount") : DomainException(message, "AMOUNT_MISMATCH")
    {
    }
}
