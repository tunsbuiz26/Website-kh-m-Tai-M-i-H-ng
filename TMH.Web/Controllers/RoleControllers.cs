using Microsoft.AspNetCore.Mvc;
using TMH.Shared.DTOs;
using TMH.Web.Services;

namespace TMH.Web.Controllers
{
    // ================================================================
    // Các controller này bảo vệ bằng cách kiểm tra Session trực tiếp.
    // Vì Web App không dùng JWT middleware, chúng ta tự kiểm tra role
    // từ Session thay vì dùng [Authorize(Roles="...")] của ASP.NET Core.
    //
    // Trong thực tế nên tạo một custom AuthorizeAttribute hoặc
    // ActionFilter để tái sử dụng, tránh lặp code ở mỗi action.
    // ================================================================

    // ----------------------------------------------------------------
    // PATIENT CONTROLLER — Bệnh nhân
    // ----------------------------------------------------------------
    public class PatientController : Controller
    {
        private bool IsPatient() =>
            HttpContext.Session.GetString("UserRole") == "Patient";

        public IActionResult Index()
        {
            if (!IsPatient()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        public IActionResult Book()
        {
            if (!IsPatient()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        public IActionResult Results()
        {
            if (!IsPatient()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        public IActionResult Profile()
        {
            if (!IsPatient()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }
    }

    // ----------------------------------------------------------------
    // DOCTOR CONTROLLER — Bác sĩ
    // ----------------------------------------------------------------
    public class DoctorController : Controller
    {
        private readonly ApiService _api;
        public DoctorController(ApiService api) { _api = api; }

        private bool IsDoctor() =>
            HttpContext.Session.GetString("UserRole") == "Doctor";

        // GET /Doctor/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDoctor()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            var list = await _api.GetAppointmentsByDateAsync(DateTime.Today);
            ViewBag.TodayDate = DateTime.Today.ToString("dd/MM/yyyy");
            return View(list ?? new List<TMH.Shared.DTOs.AppointmentDetailDto>());
        }

        // GET /Doctor/GetAppointments?date=2026-04-01 — AJAX
        [HttpGet]
        public async Task<IActionResult> GetAppointments(DateTime date)
        {
            if (!IsDoctor()) return Unauthorized();
            var list = await _api.GetAppointmentsByDateAsync(date);
            return Json(list ?? new List<TMH.Shared.DTOs.AppointmentDetailDto>());
        }

        // POST /Doctor/SaveDiagnosis — ghi chuẩn đoán + cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDiagnosis([FromBody] TMH.Shared.DTOs.UpdateAppointmentStatusDto dto)
        {
            if (!IsDoctor()) return Unauthorized();
            var result = await _api.UpdateAppointmentStatusAsync(dto);
            return Json(result);
        }
    }

    // ----------------------------------------------------------------
    // STAFF CONTROLLER — Lễ tân / Nhân viên
    // ----------------------------------------------------------------
    public class StaffController : Controller
    {
        private readonly ApiService _api;

        public StaffController(ApiService api) { _api = api; }

        private bool IsStaff() =>
            HttpContext.Session.GetString("UserRole") == "Staff";

        // GET /Staff/Dashboard — trang chính lễ tân
        public async Task<IActionResult> Dashboard()
        {
            if (!IsStaff()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");

            // Load danh sách bác sĩ để populate filter dropdown
            var doctors = await _api.GetAvailableDoctorsAsync();
            ViewBag.Doctors = doctors ?? new List<DoctorScheduleDto>();
            return View();
        }

        // GET /Staff/GetAppointments?date=2026-04-01&doctorId=1
        // AJAX — trả JSON danh sách lịch khám theo ngày
        [HttpGet]
        public async Task<IActionResult> GetAppointments(DateTime date, int? doctorId)
        {
            if (!IsStaff()) return Unauthorized();
            var list = await _api.GetAppointmentsByDateAsync(date, doctorId);
            return Json(list ?? new List<AppointmentDetailDto>());
        }

        // POST /Staff/UpdateStatus — AJAX cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateAppointmentStatusDto dto)
        {
            if (!IsStaff()) return Unauthorized();
            var result = await _api.UpdateAppointmentStatusAsync(dto);
            return Json(result);
        }

        // POST /Staff/CreateWalkIn — tạo lịch walk-in tại chỗ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWalkIn([FromBody] BookAppointmentDto dto)
        {
            if (!IsStaff()) return Unauthorized();
            var result = await _api.BookAppointmentAsync(dto);
            return Json(result);
        }
    }

    // ----------------------------------------------------------------
    // ADMIN CONTROLLER — Quản trị viên
    // ----------------------------------------------------------------
    public class AdminController : Controller
    {
        private readonly ApiService _api;
        public AdminController(ApiService api) { _api = api; }

        private bool IsAdmin() =>
            HttpContext.Session.GetString("UserRole") == "Admin";

        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        // Forward raw JSON string từ API về browser — giữ nguyên key name
        private async Task<ContentResult> ForwardJson(string apiEndpoint)
        {
            var raw = await _api.GetRawJsonAsync(apiEndpoint);
            return Content(raw ?? "{}", "application/json");
        }

        // AJAX endpoints cho Admin dashboard
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            if (!IsAdmin()) return Unauthorized();
            return await ForwardJson("api/admin/stats");
        }

        [HttpGet]
        public async Task<IActionResult> GetWeeklyStats()
        {
            if (!IsAdmin()) return Unauthorized();
            return await ForwardJson("api/admin/stats/weekly");
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            if (!IsAdmin()) return Unauthorized();
            return await ForwardJson("api/admin/users");
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctors()
        {
            if (!IsAdmin()) return Unauthorized();
            return await ForwardJson("api/admin/doctors");
        }

        [HttpGet]
        public async Task<IActionResult> GetStatsByDoctor()
        {
            if (!IsAdmin()) return Unauthorized();
            return await ForwardJson("api/admin/stats/by-doctor");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserActive(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var data = await _api.PutAsync<object>($"api/admin/users/{id}/toggle-active", new { });
            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDoctorAvailable(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var data = await _api.PutAsync<object>($"api/admin/doctors/{id}/toggle-available", new { });
            return Json(data);
        }

        // ── Lịch làm việc ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetSchedules(int? doctorId, string? from, string? to)
        {
            if (!IsAdmin()) return Unauthorized();
            var url = "api/admin/schedules?";
            if (doctorId.HasValue) url += $"doctorId={doctorId}&";
            if (!string.IsNullOrEmpty(from)) url += $"from={from}&";
            if (!string.IsNullOrEmpty(to))   url += $"to={to}&";
            return await ForwardJson(url.TrimEnd('?', '&'));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule([FromBody] object dto)
        {
            if (!IsAdmin()) return Unauthorized();
            var data = await _api.PostPublicAsync<object>("api/admin/schedules", dto);
            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var data = await _api.DeleteAsync<object>($"api/admin/schedules/{id}");
            return Json(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateScheduleBatch([FromBody] object dto)
        {
            if (!IsAdmin()) return Unauthorized();
            var data = await _api.PostPublicAsync<object>("api/admin/schedules/batch", dto);
            return Json(data);
        }
    }
}
