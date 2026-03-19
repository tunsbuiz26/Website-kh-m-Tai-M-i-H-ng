using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TMH.Shared.DTOs;
using TMH.Web.Services;
namespace TMH.Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApiService _api;

        public BookingController(ApiService api)
        {
            _api = api;
        }

        // GET /Booking/Index — trang chọn bác sĩ + khung giờ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Phải đăng nhập mới vào được
            if (HttpContext.Session.GetString("JwtToken") == null)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = "/Booking/Index" });

            var doctors = await _api.GetAvailableDoctorsAsync();
            return View(doctors ?? new List<DoctorScheduleDto>());
        }

        // POST /Booking/Book — xử lý form đặt lịch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int patientId, int scheduleId, string? note)
        {
            if (HttpContext.Session.GetString("JwtToken") == null)
                return RedirectToAction("Login", "Account");

            var dto = new BookAppointmentDto
            {
                PatientId = patientId,
                ScheduleId = scheduleId,
                Note = note
            };

            var result = await _api.BookAppointmentAsync(dto);

            if (result == null)
            {
                TempData["ErrorMessage"] = "Không kết nối được đến máy chủ.";
                return RedirectToAction("Index");
            }

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("MyAppointments");
        }

        // GET /Booking/MyAppointments — danh sách lịch khám của bệnh nhân
        [HttpGet]
        public async Task<IActionResult> MyAppointments()
        {
            if (HttpContext.Session.GetString("JwtToken") == null)
                return RedirectToAction("Login", "Account");

            var list = await _api.GetMyAppointmentsAsync();
            return View(list ?? new List<AppointmentDetailDto>());
        }

        // POST /Booking/Cancel — huỷ lịch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (HttpContext.Session.GetString("JwtToken") == null)
                return RedirectToAction("Login", "Account");

            var result = await _api.CancelAppointmentAsync(id);

            TempData[result?.Success == true ? "SuccessMessage" : "ErrorMessage"]
                = result?.Message ?? "Có lỗi xảy ra.";

            return RedirectToAction("MyAppointments");
        }
    }
}
