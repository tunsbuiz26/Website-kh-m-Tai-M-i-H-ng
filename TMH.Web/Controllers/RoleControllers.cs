using Microsoft.AspNetCore.Mvc;

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
        private bool IsDoctor() =>
            HttpContext.Session.GetString("UserRole") == "Doctor";

        public IActionResult Dashboard()
        {
            if (!IsDoctor()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        public IActionResult Schedule()
        {
            if (!IsDoctor()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        public IActionResult Patients()
        {
            if (!IsDoctor()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }
    }

    // ----------------------------------------------------------------
    // STAFF CONTROLLER — Lễ tân / Nhân viên
    // ----------------------------------------------------------------
    public class StaffController : Controller
    {
        private bool IsStaff() =>
            HttpContext.Session.GetString("UserRole") == "Staff";

        public IActionResult Dashboard()
        {
            if (!IsStaff()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        public IActionResult Appointments()
        {
            if (!IsStaff()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }
    }

    // ----------------------------------------------------------------
    // ADMIN CONTROLLER — Quản trị viên
    // Admin có thể xem tất cả, kể cả dashboard của role khác
    // ----------------------------------------------------------------
    public class AdminController : Controller
    {
        private bool IsAdmin() =>
            HttpContext.Session.GetString("UserRole") == "Admin";

        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }

        public IActionResult Reports()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied", "Account");
            return View();
        }
    }
}
