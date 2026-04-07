using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.Admin.Models;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Sales;

namespace SV22T1020488.Admin.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            // 1. Thống kê Sản phẩm (Sửa PageSize từ 0 thành 1 để tránh lỗi SQL)
            var productInput = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 1,
                SearchValue = ""
            };
            var productData = await CatalogDataService.ListProductsAsync(productInput);
            ViewBag.TotalProducts = productData.RowCount;

            // 2. Thống kê Khách hàng (Sửa PageSize từ 0 thành 1 để tránh lỗi SQL)
            var customerInput = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 1,
                SearchValue = ""
            };
            var customerData = await PartnerDataService.ListCustomersAsync(customerInput);
            ViewBag.TotalCustomers = customerData.RowCount;

            // 3. Lấy 5 Đơn hàng mới nhất để hiện lên bảng Dashboard
            var orderInput = new OrderSearchInput()
            {
                Page = 1,
                PageSize = 5,
                Status = 0, // Lấy tất cả các trạng thái
                SearchValue = ""
            };
            var orderData = await SalesDataService.ListOrdersAsync(orderInput);

            // Gán số lượng đơn hàng vào ViewBag để hiển thị lên Card "Đơn hàng mới"
            ViewBag.PendingOrders = orderData.RowCount;

            // Giả định doanh thu (Bạn có thể bổ sung hàm tính doanh thu sau nếu cần)
            ViewBag.TotalRevenue = 0;

            // Trả về danh sách 5 đơn hàng mới nhất làm Model cho View
            return View(orderData.DataItems);
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