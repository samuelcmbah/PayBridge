using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.Exceptions
{
    public sealed class InvalidEmailException(string message, string errorCode = "INVALID_EMAIL") : DomainException(message, errorCode)
    {
    }
}
