using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;

namespace SV22T1020488.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        public async Task<IActionResult> Index()
        {
            // 1. Lấy cấu hình tìm kiếm từ Session hoặc tạo mới nếu chưa có
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = PAGE_SIZE,
                    SearchValue = "",
                    CategoryID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }

            // 2. Lấy danh sách Categories cho SelectBox lọc
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 100,
                SearchValue = ""
            });
            ViewBag.Categories = categories.DataItems;

            // 3. QUAN TRỌNG: Trả về Model là 'input' để View Index biết cần load trang nào
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và lọc sản phẩm (Gọi qua AJAX)
        /// </summary>
        public async Task<IActionResult> SearchResult(ProductSearchInput input)
        {
            input.PageSize = PAGE_SIZE;
            input.SearchValue ??= "";

            // Lấy dữ liệu thực tế từ Database
            var result = await CatalogDataService.ListProductsAsync(input);

            // Lưu lại tiêu chí tìm kiếm hiện tại vào Session để khi quay lại (từ Cart/Detail) sẽ nhớ trang
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return PartialView("SearchResult", result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
    }
}