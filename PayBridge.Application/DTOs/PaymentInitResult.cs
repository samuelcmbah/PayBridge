using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public record PaymentInitResult(string Reference, string AuthorizationUrl);

}
