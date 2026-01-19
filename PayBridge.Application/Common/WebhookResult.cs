using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Application.Common
{
    public class WebhookResult
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public WebhookResultType ResultType { get; }

        private WebhookResult(bool isSuccess, WebhookResultType resultType, string? error = null)
        {
            IsSuccess = isSuccess;
            ResultType = resultType;
            Error = error;
        }

        public static WebhookResult Success() => new(true, WebhookResultType.Processed);
        public static WebhookResult Ignored(string reason) => new(true, WebhookResultType.Ignored, reason);
        public static WebhookResult Failed(string error) => new(false, WebhookResultType.Failed, error);
    }

    public enum WebhookResultType
    {
        Processed,  // Successfully processed
        Ignored,    // Valid but ignored (e.g., duplicate, non-pending payment)
        Failed      // Error occurred
    }
}
