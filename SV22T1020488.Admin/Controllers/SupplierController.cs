using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;

namespace SV22T1020488.Admin.Controllers
{

    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        private const int PAGESIZE = 10;
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm nhà cung cấp trong session
        /// </summary>
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);
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
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Tạo mới nhà cung cấp
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Sửa nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần sửa</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");
                if (string.IsNullOrWhiteSpace(data.ContactName))
                    ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";
                    return View("Edit", data);
                }

                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống, vui lòng thử lại sau.");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            // Trường hợp: Thực hiện xóa (Yêu cầu gửi lên bằng POST)
            if (HttpMethods.IsPost(Request.Method))
            {
                // Kiểm tra lại ràng buộc dữ liệu phía Server cho an toàn
                if (await PartnerDataService.IsUsedSupplierAsync(id))
                {
                    ModelState.AddModelError(string.Empty, "Không thể xóa nhà cung cấp này vì đang có các mặt hàng liên quan.");
                    var modelErr = await PartnerDataService.GetSupplierAsync(id);
                    ViewBag.CanDelete = false;
                    return View(modelErr);
                }

                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }

            // Trường hợp: Hiển thị giao diện xác nhận (Yêu cầu GET)
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra xem nhà cung cấp có xóa được không để báo cho View ẩn/hiện nút
            ViewBag.CanDelete = !await PartnerDataService.IsUsedSupplierAsync(id);

            return View(model);
        }
    }
}
