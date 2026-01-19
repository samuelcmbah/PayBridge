
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayBridge.Application.Common;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using System.Net;
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
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaystackGateway> _logger;

        // Paystack API constants
        private const int KoboMultiplier = 100;
        private const string InitializeEndpoint = "transaction/initialize";

        public PaystackGateway(
            IOptions<PaystackSettings> options,
            IHttpClientFactory httpClientFactory,
            ILogger<PaystackGateway> logger)
        {
            _secretKey = options.Value.SecretKey;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();

            // Setup HttpClient headers for Paystack
            _httpClient.BaseAddress = new Uri("https://api.paystack.co/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _secretKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public PaymentProvider Provider => PaymentProvider.Paystack;

        public async Task<Result<PaymentInitResult>> InitializeAsync(Payment payment)
        {
           
            var amountInKobo = ConvertToKobo(payment.Amount);

            var paystackPayload = new
            {
                email = payment.ExternalUserId,
                amount = amountInKobo,
                reference = payment.Reference,
                callback_url = payment.RedirectUrl,
                metadata = new
                {
                    internal_id = payment.Id,
                    app_name = payment.AppName,
                    purpose = payment.Purpose.ToString()
                }
            };

            HttpResponseMessage response;

            //Call Paystack API
            try
            {
                response = await _httpClient.PostAsJsonAsync(InitializeEndpoint, paystackPayload);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Network error while calling Paystack API - Reference: {Reference}",
                    payment.Reference
                );

                return Result<PaymentInitResult>.Failure(
                    "Unable to connect to payment provider. Please try again.",
                    "NETWORK_ERROR"
                );
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Timeout while calling Paystack API - Reference: {Reference}",
                    payment.Reference
                );

                return Result<PaymentInitResult>.Failure(
                    "Payment provider request timed out. Please try again.",
                    "TIMEOUT_ERROR"
                );
            }

            // Handle non-success status codes
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                return HandlePaystackError(response.StatusCode, errorBody);
            }

            // Parse successful response
            try
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Verify response structure
                if (!content.TryGetProperty("data", out var data))
                {
                    return Result<PaymentInitResult>.Failure(
                        "Invalid response from payment provider",
                        "INVALID_RESPONSE"
                    );
                }

                if (!data.TryGetProperty("authorization_url", out var authUrlElement))
                {
                    return Result<PaymentInitResult>.Failure(
                        "Payment provider did not return authorization URL",
                        "MISSING_AUTH_URL"
                    );
                }

                var authUrl = authUrlElement.GetString();

                if (string.IsNullOrWhiteSpace(authUrl))
                {
                    return Result<PaymentInitResult>.Failure(
                        "Payment provider returned invalid authorization URL",
                        "INVALID_AUTH_URL"
                    );
                }

                _logger.LogInformation(
                    "Successfully initialized Paystack transaction - Reference: {Reference}, AuthUrl: {AuthUrl}",
                    payment.Reference, authUrl
                );

                var result = new PaymentInitResult(payment.Reference, authUrl);
                return Result<PaymentInitResult>.Success(result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to parse Paystack response - Reference: {Reference}",
                    payment.Reference
                );

                return Result<PaymentInitResult>.Failure(
                    "Invalid response format from payment provider",
                    "PARSE_ERROR"
                );
            }
        }

        public Result<bool> VerifySignature(string jsonPayload, string signature)
        {
            // Paystack security: Hash the payload with your secret key
            try
            {
                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_secretKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jsonPayload));
                var computedSignature = Convert.ToHexString(hash).ToLower();

                var isValid = computedSignature == signature.ToLower();

                return isValid
                    ? Result<bool>.Success(true)
                    : Result<bool>.Failure("Invalid signature", "INVALID_SIGNATURE");
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    $"Signature verification failed: {ex.Message}",
                    "SIGNATURE_VERIFICATION_ERROR"
                );
            }
        }

        public Result<PaymentVerificationResult> ParseWebhook(string jsonPayload)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonPayload);

                // Validate structure
                if (!doc.RootElement.TryGetProperty("event", out var eventElement))
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Webhook missing 'event' property",
                        "INVALID_STRUCTURE"
                    );
                }

                var eventType = eventElement.GetString();

                // We only care about successful charges
                if (eventType != "charge.success")
                {
                    return Result<PaymentVerificationResult>.Failure(
                        $"Event type '{eventType}' is not processed",
                        "UNSUPPORTED_EVENT"
                    );
                }

                if (!doc.RootElement.TryGetProperty("data", out var data))
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Webhook missing 'data' property",
                        "INVALID_STRUCTURE"
                    );
                }

                // Extract reference
                if (!data.TryGetProperty("reference", out var referenceElement))
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Webhook missing payment reference",
                        "MISSING_REFERENCE"
                    );
                }

                var reference = referenceElement.GetString();
                if (string.IsNullOrWhiteSpace(reference))
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Payment reference is empty",
                        "EMPTY_REFERENCE"
                    );
                }

                // Extract amount
                if (!data.TryGetProperty("amount", out var amountElement))
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Webhook missing payment amount",
                        "MISSING_AMOUNT"
                    );
                }

                var amountInKobo = amountElement.GetInt32();
                var amountInNaira = amountInKobo / 100m;

                var result = new PaymentVerificationResult(reference, amountInNaira);
                return Result<PaymentVerificationResult>.Success(result);
            }
            catch (JsonException ex)
            {
                return Result<PaymentVerificationResult>.Failure(
                    $"Failed to parse webhook JSON: {ex.Message}",
                    "JSON_PARSE_ERROR"
                );
            }
            catch (Exception ex)
            {
                return Result<PaymentVerificationResult>.Failure(
                    $"Unexpected error parsing webhook: {ex.Message}",
                    "PARSE_ERROR"
                );
            }
        }

        // Helper Methods

        private static int ConvertToKobo(decimal amountInNaira)
        {
            return (int)(amountInNaira * KoboMultiplier);
        }

        private Result<PaymentInitResult> HandlePaystackError(HttpStatusCode statusCode, string errorBody)
        {
            // Try to parse Paystack error message
            string errorMessage = "Payment initialization failed";

            try
            {
                var errorJson = JsonDocument.Parse(errorBody);
                if (errorJson.RootElement.TryGetProperty("message", out var messageElement))
                {
                    errorMessage = messageElement.GetString() ?? errorMessage;
                }
            }
            catch
            {
                // If we can't parse error, use default message
            }

            return statusCode switch
            {
                HttpStatusCode.Unauthorized => Result<PaymentInitResult>.Failure(
                    "Payment provider authentication failed. Please contact support.",
                    "PROVIDER_AUTH_ERROR"
                ),
                HttpStatusCode.BadRequest => Result<PaymentInitResult>.Failure(
                    $"Invalid payment request: {errorMessage}",
                    "INVALID_REQUEST"
                ),
                HttpStatusCode.TooManyRequests => Result<PaymentInitResult>.Failure(
                    "Too many payment requests. Please try again in a moment.",
                    "RATE_LIMIT_ERROR"
                ),
                HttpStatusCode.InternalServerError or
                HttpStatusCode.ServiceUnavailable => Result<PaymentInitResult>.Failure(
                    "Payment provider is temporarily unavailable. Please try again.",
                    "PROVIDER_UNAVAILABLE"
                ),
                _ => Result<PaymentInitResult>.Failure(
                    $"Payment initialization failed: {errorMessage}",
                    "PROVIDER_ERROR"
                )
            };
        }
    }
}