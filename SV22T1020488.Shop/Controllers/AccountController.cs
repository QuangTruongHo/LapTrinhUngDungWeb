using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Partner;
using SV22T1020488.Models.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SV22T1020488.Shop.AppCodes; // THÊM DÒNG NÀY ĐỂ NHẬN DIỆN SELECTLISTHELPER

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
            // Nếu đã đăng nhập rồi thì không cho vào trang Login nữa, đẩy về Home
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                return View();
            }

            // 2. Mã hóa mật khẩu và kiểm tra tài khoản từ Database
            string encryptedPassword = GetMd5Hash(password);
            var userAccount = await UserAccountService.AuthorizeCustomerAsync(username, encryptedPassword);

            if (userAccount == null)
            {
                TempData["Error"] = "Email hoặc mật khẩu không chính xác hoặc tài khoản bị khóa.";
                return View();
            }

            // --- QUAN TRỌNG: LÀM SẠCH PHIÊN LÀM VIỆC CŨ ---
            // Xóa bỏ hoàn toàn Session của khách vãng lai trước khi nạp dữ liệu User chính thức
            HttpContext.Session.Clear();

            // --- 1. LƯU VÀO SESSION ĐỂ HEADER VÀ GIỎ HÀNG NHẬN DIỆN ---
            string userIdStr = userAccount.UserId ?? "0";
            HttpContext.Session.SetString("CustomerID", userIdStr);
            HttpContext.Session.SetString("CustomerName", userAccount.DisplayName ?? "");
            HttpContext.Session.SetString("CustomerAvatar", userAccount.Photo ?? "default.png");

            // --- 2. GỘP GIỎ HÀNG ---
            // Sau khi đã có CustomerID trong Session, gọi MergeCart để gộp đồ từ giỏ tạm vào giỏ User
            try
            {
                CartHelper.MergeCart(HttpContext);
            }
            catch
            {
                // Tránh việc lỗi giỏ hàng làm gián đoạn quá trình đăng nhập
            }

            // --- 3. THIẾT LẬP COOKIE XÁC THỰC (CLAIMS) ---
            var authClaims = new List<Claim>
    {
        new Claim("UserId", userIdStr),
        new Claim(ClaimTypes.Name, userAccount.DisplayName ?? ""),
        new Claim(ClaimTypes.Email, userAccount.UserName ?? ""),
        new Claim(ClaimTypes.Role, "Customer")
    };

            var identity = new ClaimsIdentity(authClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Ghi nhớ đăng nhập
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            // Thực hiện đăng nhập vào hệ thống Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // --- 4. ÉP BUỘC LƯU SESSION TRƯỚC KHI CHUYỂN TRANG ---
            // Dòng này đảm bảo Header ở trang Home sẽ đọc được CustomerID ngay lập tức
            await HttpContext.Session.CommitAsync();

            // Thông báo thành công
            TempData["Success"] = $"Chào mừng {userAccount.DisplayName} đã quay trở lại!";

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            // Xóa định danh trong Session để ngắt quyền truy cập giỏ hàng hiện tại
            HttpContext.Session.Remove("CustomerID");
            HttpContext.Session.Remove("CustomerName");

            // Đăng xuất Cookie xác thực người dùng
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
            // 1. Lấy ID từ Session hoặc từ Claims (Cookie) nếu Session bị hết hạn
            string customerIdStr = HttpContext.Session.GetString("CustomerID");

            if (string.IsNullOrEmpty(customerIdStr))
            {
                // Lấy từ Claim "UserId" đã lưu lúc đăng nhập (Login)
                customerIdStr = User.FindFirst("UserId")?.Value;
            }

            // 2. Nếu không tìm thấy ID ở cả 2 nơi -> Bắt đăng nhập lại
            if (string.IsNullOrEmpty(customerIdStr))
                return RedirectToAction("Login", "Account");

            // 3. TRUY VẤN DỮ LIỆU THẬT TỪ DATABASE
            int id = int.Parse(customerIdStr);
            var user = await PartnerDataService.GetCustomerAsync(id);

            if (user == null)
            {
                TempData["Error"] = "Tài khoản không tồn tại hoặc đã bị xóa.";
                return RedirectToAction("Index", "Home");
            }

            // 4. CẬP NHẬT LẠI SESSION (Để Header và các trang khác luôn đồng bộ dữ liệu mới nhất)
            HttpContext.Session.SetString("CustomerID", user.CustomerID.ToString());
            HttpContext.Session.SetString("CustomerName", user.CustomerName ?? "");
            HttpContext.Session.SetString("CustomerAvatar", "default.png");

            // 5. Trả về View với dữ liệu là Object User từ Database
            return View(user);
        }

        // 1. Hiển thị form sửa hồ sơ
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // 1. Lấy thông tin đăng nhập
            var userData = WebUserData.GetUserData(User);
            if (userData == null)
                return RedirectToAction("Login");

            if (!int.TryParse(userData.UserID, out int customerId))
                return RedirectToAction("Login");

            // 2. Lấy dữ liệu khách hàng từ DB
            var model = await PartnerDataService.GetCustomerAsync(customerId);
            if (model == null)
                return RedirectToAction("Login");

            // 3. Lấy danh sách tỉnh thành từ AppCodes truyền qua ViewBag
            ViewBag.ProvinceList = await SelectListHelper.Provinces();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer data)
        {
            // Kiểm tra hợp lệ cơ bản
            if (string.IsNullOrEmpty(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên không được để trống");

            if (ModelState.IsValid)
            {
                // Cập nhật vào DB
                bool result = await PartnerDataService.UpdateCustomerAsync(data);
                if (result)
                {
                    return RedirectToAction("Profile");
                }
                ModelState.AddModelError("", "Lỗi: Không thể cập nhật dữ liệu vào hệ thống.");
            }

            // Nếu thất bại (Validation lỗi), phải load lại danh sách tỉnh thành trước khi trả về View
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
            if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");

            if (await UserAccountService.CheckCustomerEmailExistsAsync(data.Email))
                ModelState.AddModelError("Email", "Email này đã có người sử dụng.");

            if (ModelState.IsValid)
            {
                try
                {
                    data.Password = GetMd5Hash(password);
                    data.ContactName = string.IsNullOrWhiteSpace(data.ContactName) ? data.CustomerName : data.ContactName;
                    data.IsLocked = false;
                    data.Province = string.IsNullOrWhiteSpace(data.Province) ? null : data.Province;
                    data.Address = data.Address ?? "";
                    data.Phone = data.Phone ?? "";

                    int id = await PartnerDataService.AddCustomerAsync(data);
                    if (id > 0)
                    {
                        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                        return RedirectToAction("Login");
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                }
            }
            return View(data);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Message"] = "Mật khẩu xác nhận không khớp.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Profile");
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            string encryptedOld = GetMd5Hash(oldPassword);
            var checkOld = await UserAccountService.AuthorizeCustomerAsync(email!, encryptedOld);

            if (checkOld == null)
            {
                TempData["Message"] = "Mật khẩu cũ không chính xác.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Profile");
            }

            string encryptedNew = GetMd5Hash(newPassword);
            bool result = await UserAccountService.ChangeCustomerPasswordAsync(email!, encryptedNew);

            if (result)
            {
                TempData["Message"] = "Đổi mật khẩu thành công!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = "Có lỗi xảy ra.";
                TempData["MessageType"] = "error";
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