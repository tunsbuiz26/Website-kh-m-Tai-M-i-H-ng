using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TMH.Shared.DTOs;

namespace TMH.Web.Services
{
    /// <summary>
    /// ApiService là lớp trung gian duy nhất giữa Web App và API.
    /// Mọi request HTTP đều đi qua đây — không viết HttpClient trực tiếp trong Controller.
    ///
    /// Lý do tập trung vào một lớp:
    ///   - Dễ thêm retry logic, logging, error handling mà không sửa nhiều chỗ.
    ///   - Controller Web App đọc gần giống Controller API: gọi service, nhận DTO, render View.
    ///   - Khi URL API thay đổi, chỉ sửa một nơi duy nhất.
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient   _http;
        private readonly IHttpContextAccessor _ctx;

        // JsonSerializerOptions tái sử dụng để tiết kiệm memory và đảm nhất quán
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true   // "token" và "Token" đều được nhận
        };

        public ApiService(HttpClient http, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx  = ctx;
        }

        // =====================================================================
        // ĐĂNG KÝ — gọi POST /api/auth/register
        // =====================================================================
        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
            => await PostAsync<AuthResponseDto>("api/auth/register", dto);

        // =====================================================================
        // ĐĂNG NHẬP — gọi POST /api/auth/login
        // =====================================================================
        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
            => await PostAsync<AuthResponseDto>("api/auth/login", dto);

        // =====================================================================
        // HELPER: POST chung — serialize DTO → gọi API → deserialize response
        // =====================================================================
        private async Task<T?> PostAsync<T>(string endpoint, object body)
        {
            // Đính token vào header nếu người dùng đã đăng nhập
            AttachToken();

            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync(endpoint, content);
            }
            catch (HttpRequestException ex)
            {
                // API không chạy hoặc mất kết nối → trả null, Controller sẽ hiển thị lỗi
                Console.Error.WriteLine($"[ApiService] Không kết nối được API: {ex.Message}");
                return default;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseJson, _jsonOpts);
        }

        // =====================================================================
        // HELPER: GET chung — dùng cho các endpoint lấy dữ liệu
        // =====================================================================
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            AttachToken();
            try
            {
                var response = await _http.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode) return default;
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOpts);
            }
            catch (HttpRequestException)
            {
                return default;
            }
        }

        // =====================================================================
        // Đính JWT token từ Session vào Authorization header
        // Session lưu token sau khi đăng nhập thành công
        // =====================================================================
        private void AttachToken()
        {
            var token = _ctx.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            else
                _http.DefaultRequestHeaders.Authorization = null;
        }
    }
}
