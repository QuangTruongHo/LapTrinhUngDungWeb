using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;

namespace SV22T1020488.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        private const int PAGESIZE = 10;
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm loại hàng trong session
        /// </summary>
        private const string CATEGORY_SEARCH = "CategorySearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);
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
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Thêm mới 1 loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới loại hàng";
            var model = new Category() { CategoryID = 0 };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật 1 loại hàng
        /// </summary>
        /// <param name="id">Mã nhà loại hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.CategoryID == 0 ? "Thêm mới loại hàng" : "Cập nhật loại hàng";
                    return View("Edit", data);
                }

                if (data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống. Vui lòng thử lại sau.");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa 1 loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            // Trường hợp: Thực hiện xóa (Yêu cầu gửi lên bằng POST)
            if (HttpMethods.IsPost(Request.Method))
            {
                // 1. Kiểm tra xem loại hàng có đang được sử dụng bởi sản phẩm nào không
                // Giả sử hàm kiểm tra là IsUsedCategoryAsync (tên có thể thay đổi tùy Service của bạn)
                if (await CatalogDataService.IsUsedCategoryAsync(id))
                {
                    ModelState.AddModelError(string.Empty, "Không thể xóa loại hàng này vì đang có sản phẩm thuộc loại hàng này.");
                    var modelErr = await CatalogDataService.GetCategoryAsync(id);
                    ViewBag.CanDelete = false; // Không cho phép hiện nút xóa nữa
                    return View(modelErr);
                }

                // 2. Thực hiện xóa khỏi Database
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }

            // Trường hợp: Hiển thị giao diện xác nhận (Yêu cầu GET)
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra khả năng xóa để báo cho View ẩn/hiện nút
            ViewBag.CanDelete = !await CatalogDataService.IsUsedCategoryAsync(id);

            return View(model);
        }
    }
}
