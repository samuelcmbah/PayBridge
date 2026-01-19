using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PayBridge.Application.IServices;
using System.IO;
using System.Threading.Tasks;

namespace PayBridge.API.Controllers;

[Route("api/webhooks")]
[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IPaymentService paymentService,
        ILogger<WebhooksController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("{provider}")]
    public async Task<IActionResult> HandleWebhook(string provider)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var jsonPayload = await reader.ReadToEndAsync();

        // Get signature header (different providers use different header names)
        var signature = provider.ToLower() switch
        {
            "paystack" => Request.Headers["x-paystack-signature"].ToString(),
            "flutterwave" => Request.Headers["verif-hash"].ToString(),
            _ => string.Empty
        };

        _logger.LogInformation(
            "Webhook received from {Provider} - Payload length: {Length}",
            provider,
            jsonPayload?.Length ?? 0
        );

        var result = await _paymentService.HandleWebhookAsync(provider, jsonPayload, signature);

        // Always return 200 to prevent webhook retries
        return Ok(new { received = true, processed = result.IsSuccess });
    }

}
