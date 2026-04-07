using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.Admin;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Security;
using System.Security.Claims;

namespace SV22T1020488.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập Email và Mật khẩu");
                return View();
            }

            var userAccount = await UserAccountService.AuthorizeAsync(username, CryptHelper.HashMD5(password));

            if (userAccount != null)
            {
                var roles = userAccount.RoleNames?.Split(',')
                                                 .Select(r => r.Trim().ToLower())
                                                 .ToList() ?? new List<string>();

                if (roles.Contains("employee") && !roles.Contains(WebUserRoles.Sales))
                {
                    roles.Add(WebUserRoles.Sales);
                }

                var userData = new WebUserData()
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,
                    Roles = roles
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal(),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    }
                );

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không chính xác");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // 1. Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            // 2. Kiểm tra khớp mật khẩu
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu mới không khớp.");
                return View();
            }

            // 3. Kiểm tra đăng nhập
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            // 4. Kiểm tra mật khẩu cũ (Giả định mật khẩu lưu trong DB là MD5)
            var userAccount = await UserAccountService.AuthorizeAsync(userData.UserName!, CryptHelper.HashMD5(oldPassword));
            if (userAccount == null)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không chính xác.");
                return View();
            }

            // 5. Thực hiện đổi mật khẩu
            bool result = await UserAccountService.ChangePasswordAsync(userData.UserName!, CryptHelper.HashMD5(newPassword));
            if (result)
            {
                ViewBag.Message = "Đổi mật khẩu thành công!";
                return View();
            }

            ModelState.AddModelError("", "Lỗi hệ thống: Không thể cập nhật mật khẩu.");
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}