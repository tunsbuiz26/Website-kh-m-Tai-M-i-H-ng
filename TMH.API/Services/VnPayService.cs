using TMH.API.Helpers;
using TMH.Shared.DTOs;

namespace TMH.API.Services
{
    public class VnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VnPaymentRequestDto model)
        {
            var tick = DateTime.Now.Ticks.ToString();
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]!);
            vnpay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan lich kham: {model.AppointmentId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:ReturnUrl"]!);
            vnpay.AddRequestData("vnp_TxnRef", tick);

            return vnpay.CreateRequestUrl(
                _config["VnPay:BaseUrl"]!,
                _config["VnPay:HashSecret"]!
            );
        }

        public VnPaymentResponseDto PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    vnpay.AddResponseData(key, value!);
            }

            var vnpSecureHash = collections["vnp_SecureHash"].ToString();
            bool isValid = vnpay.ValidateSignature(vnpSecureHash, _config["VnPay:HashSecret"]!);

            if (!isValid)
                return new VnPaymentResponseDto
                {
                    Success = false,
                    Message = "Chữ ký không hợp lệ"
                };

            var responseCode = vnpay.GetResponseData("vnp_ResponseCode");

            return new VnPaymentResponseDto
            {
                Success = responseCode == "00",
                OrderDescription = vnpay.GetResponseData("vnp_OrderInfo"),
                OrderId = vnpay.GetResponseData("vnp_TxnRef"),
                TransactionId = vnpay.GetResponseData("vnp_TransactionNo"),
                Token = vnpSecureHash,
                VnPayResponseCode = responseCode,
                Message = responseCode == "00" ? "Thanh toán thành công" : "Thanh toán thất bại"
            };
        }
    }
}