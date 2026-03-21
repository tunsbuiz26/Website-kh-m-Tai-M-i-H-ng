using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TMH.API.Data;
using TMH.API.Helpers;
using TMH.API.Services;

// ============================================================
// Program.cs — Điểm khởi động của TMH.API
//
// ASP.NET Core 6+ dùng "Minimal Hosting Model":
//   WebApplication.CreateBuilder → cấu hình DI + middleware → Run
//
// Cấu trúc gồm 2 giai đoạn rõ ràng:
//   Giai đoạn 1: builder.Services.AddXxx() — đăng ký dịch vụ vào DI container
//   Giai đoạn 2: app.UseXxx() — thiết lập pipeline xử lý HTTP request
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// GIAI ĐOẠN 1: ĐĂNG KÝ DỊCH VỤ (Dependency Injection)
// ============================================================

// --- SQL Server với Entity Framework Core ---
// Connection string lấy từ appsettings.json (không hardcode vào code)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 3)  // Tự retry nếu DB tạm thời không phản hồi
    )
);

// --- JWT Authentication ---
// Đây là phần cốt lõi: cấu hình cách ASP.NET Core đọc và xác thực JWT token
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    // Đặt JWT làm scheme mặc định cho cả xác thực lẫn challenge (yêu cầu đăng nhập)
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(secretKey),
        ValidateIssuer           = true,
        ValidIssuer              = jwtSettings["Issuer"],
        ValidateAudience         = true,
        ValidAudience            = jwtSettings["Audience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };

    // Khi token hết hạn hoặc không hợp lệ, ASP.NET Core trả về thông báo tiếng Việt
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            ctx.Response.Headers.Add("Token-Expired",
                ctx.Exception is SecurityTokenExpiredException ? "true" : "false");
            return Task.CompletedTask;
        }
    };
});

// Authorization policy dựa trên role — [Authorize(Roles="Admin")] sẽ dùng cấu hình này
builder.Services.AddAuthorization();

// --- CORS: cho phép Web App (.Web project) gọi đến API ---
// Trong môi trường production cần thay "*" bằng URL cụ thể của Web App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
        policy.WithOrigins(
                "https://localhost:7200",   // Web App dev URL
                "http://localhost:5200"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
    );
});

// --- Đăng ký các service của ứng dụng vào DI container ---
// AddScoped = tạo instance mới cho mỗi HTTP request (phù hợp cho service có DbContext)
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<VnPayService>();

builder.Services.AddControllers();

// --- Swagger UI: tài liệu API tự động, tích hợp nút "Authorize" với JWT ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "TMH Clinic API",
        Version     = "v1",
        Description = "API cho Hệ thống Phòng Khám Tai Mũi Họng — quản lý xác thực, bệnh nhân, lịch khám."
    });

    // Thêm nút "Authorize" vào Swagger UI để test endpoint cần JWT
    var securityScheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Description  = "Nhập token theo định dạng: **Bearer {token}**",
        In           = ParameterLocation.Header,
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// GIAI ĐOẠN 2: CẤU HÌNH PIPELINE HTTP (Middleware)
// Thứ tự middleware RẤT QUAN TRỌNG — sai thứ tự gây lỗi khó debug
// ============================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TMH API v1"));

    // Tự động tạo database nếu chưa tồn tại khi chạy development
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowWebApp");  // CORS phải trước Authentication

app.UseAuthentication();     // Đọc và xác thực JWT token từ header
app.UseAuthorization();      // Kiểm tra [Authorize] attributes

app.MapControllers();

app.Run();
