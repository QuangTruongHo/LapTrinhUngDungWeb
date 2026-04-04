using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Sales;
using SV22T1020488.Shop.Models;

namespace SV22T1020488.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const int PAGE_SIZE = 10;

        /// <summary>
        /// Lịch sử đơn hàng của khách hàng
        /// </summary>
        public async Task<IActionResult> History(int page = 1, string searchValue = "")
        {
            var userData = WebUserData.GetUserData(User);
            if (userData == null) return RedirectToAction("Login", "Account");

            // Khởi tạo input tìm kiếm (Lưu ý: Không gán CustomerID vì Model không có)
            var input = new OrderSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE, // Tạm thời lấy số lượng lớn để lọc
                SearchValue = searchValue ?? "",
                Status = 0, // Lấy tất cả trạng thái
                DateFrom = null,
                DateTo = null
            };

            // 1. Lấy danh sách đơn hàng từ Service
            var result = await SalesDataService.ListOrdersAsync(input);

            // 2. Lọc thủ công bằng LINQ để chỉ lấy đơn hàng của khách hàng này
            int currentCustomerId = int.Parse(userData.UserID);

            // Ép kiểu hoặc kiểm tra null an toàn cho DataItems
            var myOrders = (result?.DataItems ?? new List<OrderViewInfo>())
                           .Where(x => x.CustomerID == currentCustomerId)
                           .OrderByDescending(x => x.OrderTime)
                           .ToList();

            return View(myOrders);
        }

        /// <summary>
        /// Chi tiết một đơn hàng cụ thể
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);

            // Kiểm tra: Đơn hàng tồn tại VÀ phải thuộc về chính khách hàng đang đăng nhập
            var userData = WebUserData.GetUserData(User);
            if (order == null || userData == null || order.CustomerID.ToString() != userData.UserID)
            {
                return NotFound(); // Trả về NotFound để bảo mật, không cho biết đơn hàng có tồn tại hay không nếu không đúng chủ
            }

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Order = order;
            return View(details);
        }

        /// <summary>
        /// Xử lý đặt hàng từ giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress)
        {
            var cart = CartHelper.GetCart(HttpContext);
            if (cart.Count == 0)
            {
                TempData["Message"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            var userData = WebUserData.GetUserData(User);
            if (userData == null) return RedirectToAction("Login", "Account");

            // 1. Tạo đơn hàng mới
            var orderData = new Order()
            {
                CustomerID = int.Parse(userData.UserID),
                DeliveryProvince = deliveryProvince ?? "",
                DeliveryAddress = deliveryAddress ?? "",
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New
            };

            int orderID = await SalesDataService.AddOrderAsync(orderData);

            if (orderID > 0)
            {
                // 2. Thêm từng món hàng vào chi tiết đơn hàng
                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(new OrderDetail()
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }

                // 3. Xóa giỏ hàng và thông báo
                CartHelper.ClearCart(HttpContext);
                TempData["Message"] = "Chúc mừng! Đơn hàng #" + orderID + " đã được đặt thành công.";
                return RedirectToAction("History");
            }

            TempData["Message"] = "Đặt hàng thất bại, vui lòng kiểm tra lại thông tin.";
            return RedirectToAction("Index", "Cart");
        }
    }
}