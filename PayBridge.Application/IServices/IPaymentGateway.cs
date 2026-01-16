using PayBridge.Application.DTOs;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.IServices
{
    public interface IPaymentGateway
    {
        PaymentProvider Provider { get; }

        Task<PaymentInitResult> InitializeAsync(Payment request);

        Task<PaymentVerificationResult> VerifyAsync(string reference);
    }
}
