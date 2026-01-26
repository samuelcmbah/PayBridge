using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack.DTOs
{
    public class PaystackInitData
    {
        [JsonPropertyName("authorization_url")] // Maps JSON snake_case to C# PascalCase
        public string AuthorizationUrl { get; set; } = default!;

        [JsonPropertyName("access_code")]
        public string AccessCode { get; set; } = default!;

        public string Reference { get; set; } = default!;
    }
}
