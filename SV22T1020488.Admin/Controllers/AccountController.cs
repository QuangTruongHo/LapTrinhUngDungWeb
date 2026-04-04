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
        /// <summary>
        /// Trang đăng nhập
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập Email và Mật khẩu");
                return View();
            }

            // Kiểm tra thông tin từ DB (Mật khẩu được Hash MD5 trước khi so sánh)
            var userAccount = await UserAccountService.AuthorizeAsync(username, CryptHelper.HashMD5(password));

            if (userAccount != null)
            {
                // KHỞI TẠO DỮ LIỆU USER ĐỂ LƯU VÀO COOKIE
                var userData = new WebUserData()
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,
                    // Tách chuỗi RoleNames (ví dụ: "admin,sales") thành List<string>
                    Roles = userAccount.RoleNames?.Split(',').Select(r => r.Trim()).ToList() ?? new List<string>()
                };

                // Ghi Cookie phiên đăng nhập
                // LƯU Ý: Hàm userData.CreatePrincipal() phải chứa logic nạp Roles vào Claims
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal(),
                    new AuthenticationProperties { IsPersistent = true }
                );

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không chính xác");
            return View();
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Giao diện đăng ký tài khoản mới
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng ký tài khoản mới
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("Error", "Xác nhận mật khẩu không khớp");
                return View();
            }

            // 2. Kiểm tra Email đã tồn tại chưa
            if (await UserAccountService.CheckEmailExistsAsync(email))
            {
                ModelState.AddModelError("Error", "Email này đã được sử dụng bởi một tài khoản khác");
                return View();
            }

            // 3. Tiến hành lưu tài khoản mới
            var newUser = new UserAccount()
            {
                DisplayName = fullName,
                Email = email,
                UserName = email,
                Photo = "user.png", // Ảnh mặc định
                RoleNames = "customer" // Quyền mặc định cho người đăng ký tự do
            };

            bool result = await UserAccountService.RegisterAsync(newUser, CryptHelper.HashMD5(password));

            if (result)
            {
                TempData["Message"] = "Đăng ký tài khoản thành công. Hãy đăng nhập để tiếp tục!";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("Error", "Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.");
            return View();
        }

        /// <summary>
        /// Giao diện đổi mật khẩu
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu thực tế
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Xác nhận mật khẩu mới không khớp");
                return View();
            }

            // 2. Lấy thông tin User hiện tại từ Cookie
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            // 3. Kiểm tra mật khẩu cũ có đúng không
            var userAccount = await UserAccountService.AuthorizeAsync(userData.UserName!, CryptHelper.HashMD5(oldPassword));
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Mật khẩu cũ không chính xác");
                return View();
            }

            // 4. Cập nhật mật khẩu mới vào DB
            bool result = await UserAccountService.ChangePasswordAsync(userData.UserName!, CryptHelper.HashMD5(newPassword));
            if (result)
            {
                ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
                return View();
            }

            ModelState.AddModelError("Error", "Không thể cập nhật mật khẩu. Vui lòng thử lại sau.");
            return View();
        }

        /// <summary>
        /// Trang báo lỗi khi không có quyền truy cập
        /// </summary>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}