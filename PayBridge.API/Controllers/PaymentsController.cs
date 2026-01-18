using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayBridge.Application.DTOs;
using PayBridge.Application.IServices;
using PayBridge.Application.Services;

namespace PayBridge.API.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            this.paymentService = paymentService;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] PaymentRequest request)
        {
            var result = await paymentService.InitializePaymentAsync(request);
            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    error = result.Error,
                    errorCode = result.ErrorCode
                });
            }

            return Ok(result.Data);
        }
    }
}
