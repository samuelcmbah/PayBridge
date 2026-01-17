using Microsoft.Extensions.Options;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PayBridge.Infrastructure.PayStack
{
    public class PaystackGateway : IPaymentGateway
    {
        private readonly string _secretKey = "sk_test_xxxx"; // Use Environment Variables!
        private readonly HttpClient httpClient;

        public PaystackGateway(IOptions<PaystackSettings> options, IHttpClientFactory httpClientFactory)
        {
            _secretKey = options.Value.SecretKey;
            httpClient = httpClientFactory.CreateClient();

            // Setup HttpClient headers for Paystack
            httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public PaymentProvider Provider => PaymentProvider.Paystack;

        public async Task<PaymentInitResult> InitializeAsync(Payment payment)
        {
            var amountInKobo = (int)(payment.Amount * 100);

            // This object represents the Paystack API Body
            var paystackPayload = new
            {
                email = payment.ExternalUserId,
                amount = amountInKobo,
                reference = payment.Reference,
                callback_url = payment.RedirectUrl, 
                metadata = new
                {
                    internal_id = payment.Id, //This links the webhook back to our DB
                    app_name = payment.AppName,
                    purpose = payment.Purpose.ToString()
                }
            };

            var response = await httpClient.PostAsJsonAsync("transaction/initialize", paystackPayload);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Paystack initialization failed.");
            }

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Extract the authorization_url from Paystack's nested response data
            var authUrl = content.GetProperty("data").GetProperty("authorization_url").GetString();

            return new PaymentInitResult(payment.Reference, authUrl ?? throw new Exception("Could not get URL from Paystack"));
        }

        public Task<bool> VerifySignatureAsync(string jsonPayload, string signature)
        {
            // Paystack security: Hash the payload with your secret key
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jsonPayload));
            var result = Convert.ToHexString(hash).ToLower();

            // If our hash matches the header signature, it's a real request
            return Task.FromResult(result == signature);
        }

        public Task<PaymentVerificationResult> ParseWebhookAsync(string jsonPayload)
        {
            var doc = JsonDocument.Parse(jsonPayload);

            if (!doc.RootElement.TryGetProperty("data", out var data))
                throw new InvalidOperationException("Invalid webhook payload: missing data");

            var reference = data.GetProperty("reference").GetString();
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("Webhook reference is missing");

            var amountInKobo = data.GetProperty("amount").GetInt32();

            return Task.FromResult(
                new PaymentVerificationResult(
                    Reference: reference,
                    Amount: amountInKobo / 100m
                )
            );
        }
    }
}
