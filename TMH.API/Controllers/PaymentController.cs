using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMH.API.Services;
using TMH.Shared.DTOs;

namespace TMH.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly VnPayService _vnPayService;

        public PaymentController(VnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        // POST /api/payment/create-payment-url
        // Bệnh nhân bấm thanh toán → API trả về URL để redirect sang VNPay
        [HttpPost("create-payment-url")]
        [Authorize(Roles = "Patient")]
        public IActionResult CreatePaymentUrl([FromBody] VnPaymentRequestDto dto)
        {
            var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, dto);
            return Ok(new { url = paymentUrl });
        }

        // GET /api/payment/payment-return
        // VNPay gọi về endpoint này sau khi thanh toán
        [HttpGet("payment-return")]
        [AllowAnonymous]
        public IActionResult PaymentReturn()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            return Ok(response);
        }
    }
}