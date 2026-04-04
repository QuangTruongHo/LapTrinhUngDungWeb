using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Shop.Models;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;

namespace SV22T1020488.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        /// <summary>
        /// Trang danh sách sản phẩm (Kết hợp tìm kiếm và lọc theo loại hàng)
        /// </summary>
        public async Task<IActionResult> Index(ShopSearchInput input)
        {
            // 1. Khởi tạo input tìm kiếm sản phẩm
            var searchInput = new ProductSearchInput()
            {
                Page = input.Page <= 0 ? 1 : input.Page,
                PageSize = PAGE_SIZE,
                SearchValue = input.SearchValue ?? "",
                CategoryID = input.CategoryID,
                MinPrice = input.MinPrice,
                MaxPrice = input.MaxPrice
            };

            // 2. Lấy danh sách sản phẩm từ Database
            var result = await CatalogDataService.ListProductsAsync(searchInput);

            // 3. Lấy danh sách loại hàng để đổ vào SelectBox
            // Lưu ý: Phải lấy .DataItems để trả về List<Category> cho View có thể foreach được
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 100, // Đảm bảo số này > 0 để tránh lỗi FETCH SQL
                SearchValue = ""
            });

            // GÁN DANH SÁCH MẶT HÀNG VÀO VIEW_BAG
            ViewBag.Categories = categoryResult.DataItems;

            // 4. Lưu lại input cũ để hiển thị lại trên Form (giữ giá trị đã chọn)
            ViewBag.SearchInput = input;

            // Trả về danh sách sản phẩm (DataItems) cho View
            return View(result.DataItems);
        }

        /// <summary>
        /// Xem chi tiết sản phẩm
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index");
            }
            return View(product);
        }
    }
}