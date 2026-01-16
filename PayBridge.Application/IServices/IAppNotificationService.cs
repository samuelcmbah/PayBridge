using PayBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.IServices
{
    public interface IAppNotificationService
    {
        // Notifies the original app that payment is successful
        Task NotifyAppAsync(Payment payment);
    }
}
