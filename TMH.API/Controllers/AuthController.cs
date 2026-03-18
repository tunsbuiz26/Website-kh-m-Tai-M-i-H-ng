using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMH.API.Services;
using TMH.Shared.DTOs;

namespace TMH.API.Controllers
{
    /// <summary>
    /// AuthController xử lý tất cả HTTP request liên quan đến xác thực.
    ///
    /// Nguyên tắc thiết kế:
    ///   - Controller CHỈ làm 3 việc: nhận request, gọi service, trả response.
    ///   - Không viết business logic trực tiếp trong controller.
    ///   - Trả về ActionResult chuẩn HTTP (200, 400, 401, 403, 409...).
    ///
    /// [ApiController] tự động:
    ///   - Validate ModelState (Data Annotations trong DTO)
    ///   - Trả 400 Bad Request nếu validation thất bại
    ///   - Bind JSON body vào parameter tự động
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]   // → base route: /api/auth
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger      = logger;
        }

        // =====================================================================
        // POST /api/auth/register
        // Công khai — không cần token
        // =====================================================================

        /// <summary>
        /// Đăng ký tài khoản bệnh nhân mới.
        /// Trả về JWT token ngay sau khi đăng ký thành công (auto-login).
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // ModelState đã được [ApiController] validate tự động.
            // Nếu đến được đây nghĩa là DTO hợp lệ về mặt format.

            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
            {
                // 409 Conflict = resource (username/email) đã tồn tại
                return Conflict(result);
            }

            _logger.LogInformation("Đăng ký thành công: {Username}", dto.Username);
            return Ok(result);
        }

        // =====================================================================
        // POST /api/auth/login
        // Công khai — không cần token
        // =====================================================================

        /// <summary>
        /// Đăng nhập, nhận về JWT token.
        /// Token này sẽ được Web App đính vào header "Authorization: Bearer {token}"
        /// của mọi request tiếp theo đến các endpoint cần xác thực.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
            {
                // 401 Unauthorized = sai credentials
                return Unauthorized(result);
            }

            _logger.LogInformation("Đăng nhập thành công: {User}", dto.UsernameOrEmail);
            return Ok(result);
        }

        // =====================================================================
        // GET /api/auth/profile
        // Yêu cầu đăng nhập — bất kỳ role nào
        // =====================================================================

        /// <summary>
        /// Lấy thông tin của user hiện tại từ JWT token.
        /// [Authorize] không chỉ định Roles nghĩa là chấp nhận mọi role đã đăng nhập.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            // User là ClaimsPrincipal được ASP.NET Core tự inject từ JWT middleware
            var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst("username")?.Value;
            var fullname = User.FindFirst("fullname")?.Value;
            var role     = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new { userId, username, fullname, role });
        }

        // =====================================================================
        // GET /api/auth/admin-only
        // Chỉ Admin — ví dụ minh hoạ phân quyền theo role
        // =====================================================================

        /// <summary>
        /// Ví dụ endpoint chỉ Admin truy cập được.
        /// [Authorize(Roles = "Admin")] sẽ trả 403 Forbidden nếu token không có role Admin.
        /// Có thể kết hợp nhiều role: [Authorize(Roles = "Admin,Doctor")]
        /// </summary>
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new { message = "Chào Quản trị viên! Bạn có toàn quyền hệ thống." });
        }

        [HttpGet("doctor-staff")]
        [Authorize(Roles = "Admin,Doctor,Staff")]
        public IActionResult DoctorAndStaff()
        {
            return Ok(new { message = "Khu vực dành cho Bác sĩ và Nhân viên phòng khám." });
        }

        [HttpGet("patient-area")]
        [Authorize(Roles = "Patient")]
        public IActionResult PatientArea()
        {
            return Ok(new { message = "Cổng bệnh nhân — xem lịch khám và kết quả của bạn." });
        }
        [HttpGet("genhash")]
        [AllowAnonymous]
        public IActionResult GenHash()
        {
            string hash = BCrypt.Net.BCrypt.HashPassword("TMH@123456", workFactor: 12);
            return Ok(new { hash });
        }
    }
}
