using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020488.BusinessLayers;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES) ---

// Hỗ trợ MVC (Controller & View)
builder.Services.AddControllersWithViews();

// Hỗ trợ truy cập HttpContext trong các lớp Helper (như CartHelper)
builder.Services.AddHttpContextAccessor();

// Cấu hình Session (Dùng để lưu trữ Giỏ hàng)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình Authentication (Dùng Cookie để duy trì trạng thái đăng nhập)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SV22T1020488.Shop.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

var app = builder.Build();

// --- 2. KẾT NỐI DATABASE (Sử dụng file Configuration của bạn) ---

// Lấy chuỗi kết nối từ file appsettings.json
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";

// Gọi hàm Initialize từ file Configuration bạn đã gửi để "bơm" connection string vào hệ thống
Configuration.Initialize(connectionString);

// --- 3. CẤU HÌNH PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// THỨ TỰ BẮT BUỘC: Session -> Authentication -> Authorization
app.UseSession();         // Kích hoạt Session
app.UseAuthentication();  // Kích hoạt Xác thực (Đăng nhập)
app.UseAuthorization();   // Kích hoạt Phân quyền

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();