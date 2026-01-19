using PayBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.IServices
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByReferenceAsync(string reference);
        Task<Payment?> GetByExternalReferenceAsync(string appName, string externalReference); 
        Task SaveChangesAsync();
    }
}
