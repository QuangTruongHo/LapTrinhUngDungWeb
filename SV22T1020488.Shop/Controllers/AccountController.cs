using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class AccountController : Controller
{
    // [GET] Trang đăng nhập
    public IActionResult Login() => View();

    // [POST] Xử lý đăng nhập
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        // 1. Kiểm tra Email/Password trong DB (Sử dụng Service của bạn)
        // var user = UserAccountService.Authorize(email, password);
        // if (user == null) { 
        //    ModelState.AddModelError("", "Sai thông tin đăng nhập"); 
        //    return View(); 
        // }

        // 2. Tạo Identity (Lưu thông tin vào Cookie)
        var claims = new List<Claim> {
            new Claim(ClaimTypes.Name, "Tên Khách Hàng"),
            new Claim(ClaimTypes.Email, email),
            new Claim("UserID", "123")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    // [GET] Đăng ký tài khoản
    public IActionResult Register() => View();

    // [GET] Thoát
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}