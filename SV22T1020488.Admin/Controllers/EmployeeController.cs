using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.HR;

namespace SV22T1020488.Admin.Controllers
{
    [Authorize(Roles = WebUserRoles.Administrator)]
    public class EmployeeController : Controller
    {
        private const int PAGESIZE = 10;
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm nhân viên trong session
        /// </summary>
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Thêm mới 1 nhân viên
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa 1 nhân viên
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpMethods.IsPost(Request.Method))
            {
                if (await HRDataService.IsUsedEmployeeAsync(id))
                {
                    ModelState.AddModelError(string.Empty, "Không thể xóa nhân viên này vì đã có dữ liệu liên quan (lập đơn hàng).");
                    var modelErr = await HRDataService.GetEmployeeAsync(id);
                    ViewBag.CanDelete = false;
                    return View(modelErr);
                }

                await HRDataService.DeleteEmployeeAsync(id);
                return RedirectToAction("Index");
            }

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.CanDelete = !await HRDataService.IsUsedEmployeeAsync(id);
            return View(model);
        }

        /// <summary>
        /// Giao diện đổi mật khẩu
        /// </summary>
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        /// <summary>
        /// Xử lý lưu mật khẩu
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePassword(int employeeId, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp hoặc đang để trống.");
                var emp = await HRDataService.GetEmployeeAsync(employeeId);
                return View("ChangePassword", emp);
            }

            bool result = await HRDataService.ChangePasswordAsync(employeeId, newPassword);

            if (result)
            {
                // Sử dụng định danh riêng PasswordSuccess để tránh hiện nhầm bên trang Role
                TempData["PasswordSuccess"] = "Đã đổi mật khẩu nhân viên thành công.";
                return RedirectToAction("ChangePassword", new { id = employeeId });
            }
            else
            {
                ModelState.AddModelError("Error", "Lỗi hệ thống khi cập nhật mật khẩu vào cơ sở dữ liệu.");
                var emp = await HRDataService.GetEmployeeAsync(employeeId);
                return View("ChangePassword", emp);
            }
        }

        /// <summary>
        /// Giao diện phân quyền cho nhân viên
        /// </summary>
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Phân quyền nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        /// <summary>
        /// Xử lý lưu phân quyền (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(int employeeId, string[] roles)
        {
            var model = await HRDataService.GetEmployeeAsync(employeeId);
            if (model == null)
                return RedirectToAction("Index");

            model.RoleNames = (roles != null && roles.Length > 0) ? string.Join(",", roles) : "";

            bool result = await HRDataService.UpdateEmployeeAsync(model);

            if (result)
            {
                // Sử dụng định danh riêng RoleSuccess để tránh hiện nhầm bên trang Password
                TempData["RoleSuccess"] = $"Đã cập nhật quyền cho nhân viên {model.FullName} thành công.";
                return RedirectToAction("ChangeRole", new { id = employeeId });
            }
            else
            {
                ModelState.AddModelError("Error", "Không thể cập nhật phân quyền vào hệ thống.");
                return View("ChangeRole", model);
            }
        }
    }
}