using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Partner;
using SV22T1020488.Models.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SV22T1020488.Shop.AppCodes;

namespace SV22T1020488.Shop.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        /// Giao diện đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        [HttpPost]
      
        public async Task<IActionResult> Login(string username, string password)
        {
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                return View();
            }

            
            string encryptedPassword = GetMd5Hash(password);
            var userAccount = await UserAccountService.AuthorizeCustomerAsync(username, encryptedPassword);

            if (userAccount == null)
            {
                TempData["Error"] = "Email hoặc mật khẩu không chính xác hoặc tài khoản bị khóa.";
                return View();
            }

            
            HttpContext.Session.Clear();

            
            string userIdStr = userAccount.UserId ?? "0";
            HttpContext.Session.SetString("CustomerID", userIdStr);
            HttpContext.Session.SetString("CustomerName", userAccount.DisplayName ?? "");
            HttpContext.Session.SetString("CustomerAvatar", userAccount.Photo ?? "default.png");

            
            try
            {
                CartHelper.MergeCart(HttpContext);
            }
            catch
            {
                
            }


            var authClaims = new List<Claim>
            {
                new Claim("UserID", userIdStr),
                new Claim(ClaimTypes.Name, userAccount.DisplayName ?? ""),
                new Claim(ClaimTypes.Email, userAccount.UserName ?? ""),
                
                new Claim(ClaimTypes.MobilePhone, userAccount.Phone ?? ""),
                new Claim(ClaimTypes.Role, "Customer")
            };

            var identity = new ClaimsIdentity(authClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Ghi nhớ đăng nhập
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

           
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

           
            await HttpContext.Session.CommitAsync();

            
            TempData["Success"] = $"Chào mừng {userAccount.DisplayName} đã quay trở lại!";

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            
            HttpContext.Session.Remove("CustomerID");
            HttpContext.Session.Remove("CustomerName");

           
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Success"] = "Đã đăng xuất. Giỏ hàng của bạn đã được lưu lại trình duyệt.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Hồ sơ cá nhân
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
           
            string customerIdStr = HttpContext.Session.GetString("CustomerID");

            if (string.IsNullOrEmpty(customerIdStr))
            {
               
                customerIdStr = User.FindFirst("UserId")?.Value;
            }

            
            if (string.IsNullOrEmpty(customerIdStr))
                return RedirectToAction("Login", "Account");

            
            int id = int.Parse(customerIdStr);
            var user = await PartnerDataService.GetCustomerAsync(id);

            if (user == null)
            {
                TempData["Error"] = "Tài khoản không tồn tại hoặc đã bị xóa.";
                return RedirectToAction("Index", "Home");
            }

            
            HttpContext.Session.SetString("CustomerID", user.CustomerID.ToString());
            HttpContext.Session.SetString("CustomerName", user.CustomerName ?? "");
            HttpContext.Session.SetString("CustomerAvatar", "default.png");

            
            return View(user);
        }

       
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            
            var userData = WebUserData.GetUserData(User);
            if (userData == null)
                return RedirectToAction("Login");

            if (!int.TryParse(userData.UserID, out int customerId))
                return RedirectToAction("Login");

            
            var model = await PartnerDataService.GetCustomerAsync(customerId);
            if (model == null)
                return RedirectToAction("Login");

            
            ViewBag.ProvinceList = await SelectListHelper.Provinces();

            return View(model);
        }

        [HttpPost]
        
        public async Task<IActionResult> Edit(Customer data)
        {
            
            if (string.IsNullOrEmpty(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên không được để trống");

            if (ModelState.IsValid)
            {
                
                bool result = await PartnerDataService.UpdateCustomerAsync(data);
                if (result)
                {
                    return RedirectToAction("Profile");
                }
                ModelState.AddModelError("", "Lỗi: Không thể cập nhật dữ liệu vào hệ thống.");
            }

            
            ViewBag.ProvinceList = await SelectListHelper.Provinces();
            return View(data);
        }

        /// <summary>
        /// Xử lý đăng ký
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer data, string password, string confirmPassword)
        {
            // 1. Kiểm tra mật khẩu khớp nhau
            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
            }

            // 2. Kiểm tra định dạng số điện thoại (tùy chọn nhưng nên có)
            if (string.IsNullOrWhiteSpace(data.Phone))
            {
                ModelState.AddModelError("Phone", "Vui lòng nhập số điện thoại.");
            }
            else if (data.Phone.Length < 10 || data.Phone.Length > 11)
            {
                ModelState.AddModelError("Phone", "Số điện thoại phải từ 10 đến 11 số.");
            }

            // 3. Kiểm tra Email đã tồn tại chưa
            if (await UserAccountService.CheckCustomerEmailExistsAsync(data.Email))
            {
                ModelState.AddModelError("Email", "Email này đã có người sử dụng.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Mã hóa mật khẩu MD5
                    data.Password = GetMd5Hash(password);

                    // Gán các giá trị mặc định nếu để trống
                    data.ContactName = string.IsNullOrWhiteSpace(data.ContactName) ? data.CustomerName : data.ContactName;
                    data.IsLocked = false;

                    // Xử lý các trường có thể null để tránh lỗi Database
                    data.Province = string.IsNullOrWhiteSpace(data.Province) ? "" : data.Province;
                    data.Address = data.Address ?? "";

                    // Số điện thoại lúc này đã được nhận từ form qua tham số 'data'
                    data.Phone = data.Phone?.Trim() ?? "";

                    // Lưu vào Database thông qua Service
                    int id = await PartnerDataService.AddCustomerAsync(data);

                    if (id > 0)
                    {
                        TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập để trải nghiệm.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Đăng ký không thành công. Vui lòng thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu cần thiết
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                }
            }

            // Nếu có lỗi, trả về View kèm dữ liệu đã nhập để người dùng không phải nhập lại
            return View(data);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // 1. Kiểm tra mật khẩu mới và xác nhận
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["Message"] = "Mật khẩu xác nhận không khớp.";
                TempData["MessageType"] = "error";
                // Flag này để View biết cần bật lại Modal ngay lập tức
                TempData["OpenModal"] = true;
                return RedirectToAction("Profile");
            }

            // 2. Lấy Email từ Claims (Sử dụng User.Identity.Name hoặc Claim tùy cấu hình của bạn)
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            // 3. Kiểm tra mật khẩu cũ (Mã hóa MD5 để so khớp)
            string encryptedOld = GetMd5Hash(oldPassword);
            var checkOld = await UserAccountService.AuthorizeCustomerAsync(email, encryptedOld);

            if (checkOld == null)
            {
                TempData["Message"] = "Mật khẩu cũ không chính xác.";
                TempData["MessageType"] = "error";
                TempData["OpenModal"] = true;
                return RedirectToAction("Profile");
            }

            // 4. Thực hiện đổi mật khẩu
            string encryptedNew = GetMd5Hash(newPassword);
            bool result = await UserAccountService.ChangeCustomerPasswordAsync(email, encryptedNew);

            if (result)
            {
                TempData["Message"] = "Đổi mật khẩu thành công!";
                TempData["MessageType"] = "success";
                // Khi thành công, không gửi OpenModal để Modal đóng lại, chỉ hiện thông báo
            }
            else
            {
                TempData["Message"] = "Có lỗi xảy ra trong quá trình cập nhật.";
                TempData["MessageType"] = "error";
                TempData["OpenModal"] = true;
            }

            return RedirectToAction("Profile");
        }

        private string GetMd5Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes).ToLower();
            }
        }

        [HttpGet]
        public IActionResult Register() => View();
    }
}