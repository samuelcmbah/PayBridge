using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Infrastructure.PayStack;

public class PaystackHttpClient(
    HttpClient httpClient,
    ILogger<PaystackHttpClient> logger)
{
    private readonly HttpClient httpClient = httpClient;
    private readonly ILogger<PaystackHttpClient> logger = logger;

    /// <summary>
    /// Initializes a transaction with Paystack
    /// </summary>
    public Task<HttpResponseMessage> InitializeTransactionAsync(object payload)
    {
        logger.LogDebug("Calling Paystack Initialize Transaction API");
        return httpClient.PostAsJsonAsync("transaction/initialize", payload);
    }

    /// <summary>
    /// Verifies a transaction with Paystack
    /// </summary>
    public Task<HttpResponseMessage> VerifyTransactionAsync(string reference)
    {
        logger.LogDebug("Verifying transaction {Reference}", reference);
        return httpClient.GetAsync($"transaction/verify/{reference}");
    }

    // Add more Paystack-specific endpoints as needed
    // - Charge authorization
    // - Create refund
    // - etc.
}

