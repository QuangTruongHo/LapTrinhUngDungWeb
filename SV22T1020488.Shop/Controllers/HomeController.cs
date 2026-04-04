using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using SV22T1020488.Shop.Models;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Catalog; // QUAN TRỌNG: Thêm dòng này để nhận diện ProductSearchInput

namespace SV22T1020488.Shop.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            // Bây giờ bạn có thể khởi tạo trực tiếp vì đã using namespace ở trên
            var input = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 8,
                SearchValue = "",
                CategoryID = 0,  // Khởi tạo các giá trị mặc định nếu cần
                SupplierID = 0,
                MinPrice = 0,
                MaxPrice = 0
            };

            var result = await CatalogDataService.ListProductsAsync(input);

            // Truyền danh sách sản phẩm sang View
            return View(result.DataItems);
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