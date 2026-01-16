using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayBridge.Application.IServices;

namespace PayBridge.API.Controllers
{
    [Route("api/webhooks")]
    [ApiController]
    public class PaystackWebhookController : ControllerBase
    {
        private readonly IPaymentService paymentService;

        public PaystackWebhookController(IPaymentService paymentService)
        {
            this.paymentService = paymentService;
        }

        [HttpPost("paystack")]
        public async Task<IActionResult> Handle()
        {
            // Get the raw JSON body
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            // Get the Paystack Signature header
            var signature = Request.Headers["x-paystack-signature"].ToString();

            await paymentService.HandleWebhookAsync("Paystack", json, signature);

            return Ok(); // Always return 200 to Paystack
        }
    }
}
