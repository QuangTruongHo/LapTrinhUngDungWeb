using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using SV22T1020488.Shop.Models;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;

namespace SV22T1020488.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Trang chủ: Hiển thị giao diện tìm kiếm và danh sách sản phẩm mặc định
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // 1. Khởi tạo đối tượng chứa tiêu chí tìm kiếm mặc định
            // Đối tượng này sẽ được gửi sang View để binding vào các Form tìm kiếm
            var input = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 12,
                SearchValue = "",
                CategoryID = 0,
                MinPrice = 0,
                MaxPrice = 0
            };

            // 2. Lấy danh sách Categories để đổ vào dropdown list (SelectBox)
            // Sử dụng await để xử lý bất đồng bộ, tránh làm treo ứng dụng
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 100, // Lấy tối đa 100 loại hàng
                SearchValue = ""
            });

            // Gán danh sách vào ViewBag để hiển thị ở Layout hoặc View
            ViewBag.Categories = categoryResult.DataItems;

            // 3. TRUYỀN Model 'input' vào View
            // Điều này cực kỳ quan trọng để View có @model ProductSearchInput không bị lỗi
            return View(input);
        }

        /// <summary>
        /// Xử lý tìm kiếm sản phẩm (thường được gọi qua Ajax)
        /// </summary>
        public IActionResult Search(ProductSearchInput input)
        {
            // Lưu lại điều kiện tìm kiếm vào Session nếu cần thiết 
            // (Tùy thuộc vào logic phân trang của bạn)
            return View(input);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}