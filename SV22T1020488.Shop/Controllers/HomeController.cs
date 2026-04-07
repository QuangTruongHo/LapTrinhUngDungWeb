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
        public async Task<IActionResult> Index(ProductSearchInput input)
        {
            // 1. Đảm bảo các thông số phân trang mặc định nếu URL không có
            if (input.Page <= 0) input.Page = 1;
            if (input.PageSize <= 0) input.PageSize = 12;

            // Xử lý chuỗi tìm kiếm tránh bị null (giúp binding lên Form đẹp hơn)
            input.SearchValue ??= "";

            // 2. Lấy danh sách Categories để đổ vào dropdown list (SelectBox)
            // Sử dụng await để xử lý bất đồng bộ
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = 100, // Lấy tối đa 100 loại hàng để lọc
                SearchValue = ""
            });

            // Gán danh sách vào ViewBag để hiển thị ở các thẻ <select>
            ViewBag.Categories = categoryResult.DataItems;

            // 3. TRUYỀN Model 'input' ngược lại vào View.
            // Việc này giúp @Model.SearchValue hay @Model.CategoryID 
            // trong file .cshtml hiển thị đúng những gì người dùng vừa nhập trên URL.
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