using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack
{
    //uses the init keyword so that the values can only be set during startup.
    public record PaystackSettings
    {
        public string SecretKey { get; init; } = default!;
        public string PublicKey { get; init; } = default!;
        public string WebhookSecret { get; init; } = default!;
    }
}
