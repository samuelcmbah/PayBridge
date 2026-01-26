
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayBridge.Application.Common;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Domain.Entities;
using PayBridge.Domain.Enums;
using PayBridge.Infrastructure.PayStack.DTOs;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PayBridge.Infrastructure.PayStack
{
    public class PaystackGateway(
        PaystackHttpClient httpClient,
        IOptions<PaystackSettings> options,
        ILogger<PaystackGateway> logger) : IPaymentGateway
    {
        private readonly PaystackHttpClient _httpClient = httpClient;
        private readonly string _secretKey = options.Value.SecretKey;
        private readonly ILogger<PaystackGateway> _logger = logger;

        private const int KoboMultiplier = 100;


        public PaymentProvider Provider => PaymentProvider.Paystack;

        public async Task<Result<PaymentInitResult>> InitializeAsync(Payment payment)
        {

            try
            {
                var paystackPayload = BuildInitializePayload(payment);
                var response = await _httpClient.InitializeTransactionAsync(paystackPayload);

                // Handle non-success status codes
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();

                    return HandlePaystackError(response.StatusCode, errorBody);
                }

                var responseDto = await response.Content.ReadFromJsonAsync<PaystackInitResponse>();

                if(responseDto?.Data?.AuthorizationUrl == null)
                {
                    return Result<PaymentInitResult>.Failure("Provider missinig auth URL", "MISSING_AUTH_URL");
                }

                var authUrl = responseDto.Data.AuthorizationUrl;
                _logger.LogInformation(
                    "Successfully initialized Paystack transaction - Reference: {Reference}, AuthUrl: {AuthUrl}",
                    payment.Reference.Value, authUrl
                );

                var result = new PaymentInitResult(payment.Reference.Value, authUrl);
                return Result<PaymentInitResult>.Success(result);

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
                    payment.Reference.Value
                );

                return Result<PaymentInitResult>.Failure(
                    "Payment provider request timed out. Please try again.",
                    "TIMEOUT_ERROR"
                );
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to parse Paystack response - Reference: {Reference}",
                    payment.Reference.Value
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
                var payload = JsonSerializer.Deserialize<PaystackWebhookPayload>(
                        jsonPayload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if(payload?.Data == null)
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Invalid webhook data",
                        "INVALID_STRUCTURE"
                    );
                }

                if(payload.Event != "charge.success")
                {
                    return Result<PaymentVerificationResult>.Failure(
                        $"Event type '{payload.Event}' is not processed",
                        "UNSUPPORTED_EVENT"
                    );
                }

                if (string.IsNullOrWhiteSpace(payload.Data.Reference))
                {
                    return Result<PaymentVerificationResult>.Failure(
                        "Payment reference is empty",
                        "EMPTY_REFERENCE"
                    );
                }

                var amountInNaira = payload.Data.Amount / 100m;

                var result = new PaymentVerificationResult(payload.Data.Reference, amountInNaira);
                return Result<PaymentVerificationResult>.Success(result);

            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Paystack webhook");
                return Result<PaymentVerificationResult>.Failure(
                    "Invalid JSON format",
                    "JSON_PARSE_ERROR"
                );
            }
            
        }

        #region Private Helper Methods

        private object BuildInitializePayload(Payment payment)
        {
            return new
            {
                email = payment.ExternalUserId.Value,
                amount = ConvertToKobo(payment.Amount.Amount),
                reference = payment.Reference.Value,
                callback_url = payment.RedirectUrl.Value,
                metadata = new
                {
                    internal_id = payment.Id,
                    app_name = payment.AppName,
                    purpose = payment.Purpose.ToString()
                }
            };
        }
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
        #endregion
    }
}