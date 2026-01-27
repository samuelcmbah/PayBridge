using FluentValidation;
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
        private readonly IValidator<PaymentRequest> validator;

        public PaymentsController(IPaymentService paymentService, IValidator<PaymentRequest> validator)
        {
            this.paymentService = paymentService;
            this.validator = validator;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] PaymentRequest request)
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        property = e.PropertyName,
                        message = e.ErrorMessage,
                        errorCode = e.ErrorCode
                    })
                });
            }

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
