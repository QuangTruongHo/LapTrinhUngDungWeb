using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Shop;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES) ---

// Hỗ trợ MVC (Controller & View)
builder.Services.AddControllersWithViews();

// Hỗ trợ truy cập HttpContext trong các lớp Static/Helper (Rất quan trọng)
builder.Services.AddHttpContextAccessor();

// Cấu hình Session - TĂNG THỜI GIAN VÀ ĐẢM BẢO COOKIE LUÔN ĐƯỢC CHẤP NHẬN
builder.Services.AddDistributedMemoryCache(); // Cần thiết để Session hoạt động ổn định
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Tăng lên 60 phút cho đồng bộ với Auth
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Bắt buộc phải có để Session chạy khi chưa nhấn đồng ý Cookie
    options.Cookie.Name = "SV22T1020488.Shop.Session";
});

// Cấu hình Authentication (Sử dụng Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SV22T1020488.Shop.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// --- 2. KHỞI TẠO DỮ LIỆU & CONTEXT ---

// Khởi tạo Connection String cho Business Layer
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";
SV22T1020488.BusinessLayers.Configuration.Initialize(connectionString);

// KÍCH HOẠT ApplicationContext (Nếu bạn dùng class static để quản lý Session)
var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
ApplicationContext.Configure(httpContextAccessor);


// --- 3. CẤU HÌNH PIPELINE (MIDDLEWARE) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // CSS, JS, Images

app.UseRouting();

// THỨ TỰ MIDDLEWARE CỰC KỲ QUAN TRỌNG:
app.UseSession();        // 1. Phải chạy Session trước để Header có dữ liệu
app.UseAuthentication(); // 2. Sau đó mới đến xác thực
app.UseAuthorization();  // 3. Cuối cùng là phân quyền

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();