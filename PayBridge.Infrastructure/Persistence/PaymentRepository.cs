using Microsoft.EntityFrameworkCore;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.Persistence
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext context;

        public PaymentRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task AddAsync(Payment payment)
        {
            await context.Payments.AddAsync(payment);
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            var payment = await context.Payments
                .FirstOrDefaultAsync(p => p.Reference == reference);
            return payment;
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}
