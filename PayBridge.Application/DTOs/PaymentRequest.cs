using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.DTOs
{
    public record PaymentRequest
    (
        string ExternalUserId,  // UserId from SportStore / ExpenseVista
        decimal Amount,
        PaymentPurpose Purpose,
        PaymentProvider Provider,
        string CallbackUrl,
        string AppName,
        string ExternalReference // The OrderId from the app
    );
}
