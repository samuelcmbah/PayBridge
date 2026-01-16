using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public record PaymentVerificationResult
    (
        string Reference,
        decimal Amount
    );
}
