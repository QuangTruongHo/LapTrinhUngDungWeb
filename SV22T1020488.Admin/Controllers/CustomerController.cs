using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;
using System;
using System.Threading.Tasks;

namespace SV22T1020488.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CustomerController : Controller
    {
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới khách hàng";
            var model = new Customer()
            {
                CustomerID = 0,
                IsLocked = false
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                // 1. Kiểm tra dữ liệu đầu vào (Validation)
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không được bỏ trống");
                else
                {
                    bool isValid = await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID);
                    if (!isValid)
                        ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng");
                }

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.CustomerID == 0 ? "Thêm mới khách hàng" : "Cập nhật khách hàng";
                    return View("Edit", data);
                }

                // 2. Làm sạch dữ liệu
                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                data.Phone = data.Phone ?? "";
                data.Address = data.Address ?? "";

                // 3. Xử lý lưu dữ liệu
                if (data.CustomerID == 0)
                {
                    // KHI THÊM MỚI: Sử dụng CryptHelper để mã hóa mật khẩu mặc định "123456"
                    // Đảm bảo namespace chứa CryptHelper đã được reference đúng
                    data.Password = CryptHelper.HashMD5("123456");
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    // KHI CẬP NHẬT THÔNG TIN: 
                    // Lấy lại mật khẩu hiện tại từ database để tránh việc bị null hoặc mất mật khẩu cũ
                    var existingCustomer = await PartnerDataService.GetCustomerAsync(data.CustomerID);
                    if (existingCustomer != null)
                    {
                        data.Password = existingCustomer.Password;
                    }
                    await PartnerDataService.UpdateCustomerAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết: ex.Message
                ModelState.AddModelError("Error", "Có lỗi xảy ra trong quá trình lưu dữ liệu. Vui lòng thử lại.");
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.CanDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
            return View(model);
        }

        /// <summary>
        /// Giao diện đổi mật khẩu (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu khách hàng";
            return View(model);
        }

        /// <summary>
        /// Thực hiện lưu mật khẩu (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null) return RedirectToAction("Index");

            // --- KIỂM TRA VALIDATION ---
            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới.");

            if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không khớp.");

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            // --- THỰC HIỆN ĐỔI MẬT KHẨU CÓ MÃ HÓA ---
            // QUAN TRỌNG: Mã hóa mật khẩu mới sang MD5 trước khi truyền vào Service
            string encryptedPassword = CryptHelper.HashMD5(newPassword);

            bool isUpdated = await PartnerDataService.ChangeCustomerPasswordAsync(id, encryptedPassword);

            if (isUpdated)
            {
                TempData["PasswordSuccess"] = $"Đổi mật khẩu cho khách hàng {customer.CustomerName} thành công!";
                return RedirectToAction("ChangePassword", new { id = id });
            }
            else
            {
                ModelState.AddModelError("", "Lỗi hệ thống: Không thể cập nhật dữ liệu mật khẩu mới.");
                return View(customer);
            }
        }
    }
}