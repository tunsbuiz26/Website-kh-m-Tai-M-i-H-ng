using Microsoft.AspNetCore.Mvc;

namespace TMH.Web.Controllers
{
    /// <summary>
    /// HomeController phục vụ trang chủ công khai (không cần đăng nhập).
    /// Nó cũng cung cấp dữ liệu để _Layout.cshtml hiển thị đúng trạng thái
    /// header: "Đăng nhập / Đăng ký" khi chưa đăng nhập,
    ///         "Xin chào [Tên]  |  Đăng xuất" khi đã đăng nhập.
    /// </summary>
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Truyền trạng thái đăng nhập vào ViewBag để _Layout.cshtml sử dụng.
            // ViewBag là dynamic object — dữ liệu được truyền từ Controller sang View trong cùng request.
            ViewBag.IsLoggedIn = HttpContext.Session.GetString("JwtToken") != null;
            ViewBag.UserName   = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole   = HttpContext.Session.GetString("UserRole");
            return View();
        }

        // Trang tĩnh: Giới thiệu phòng khám
        public IActionResult About() => View();

        // Trang tĩnh: Liên hệ
        public IActionResult Contact() => View();

        // Trang tĩnh: Hướng dẫn đặt lịch
        public IActionResult HuongDan() => View();
    }
}
