using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.Exceptions
{
    public sealed class InvalidUrlException(string message, string errorCode = "INVALID_URL") : DomainException(message, errorCode)
    {
    }
}
