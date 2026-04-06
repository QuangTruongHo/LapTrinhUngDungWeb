using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Sales;
using SV22T1020488.Shop.Models;
using SV22T1020488.Shop.AppCodes;
using System.Security.Claims;

namespace SV22T1020488.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const int PAGE_SIZE = 10;

        /// <summary>
        /// Giao diện nhập thông tin giao hàng (Địa chỉ, Tỉnh thành)
        /// Hỗ trợ cả giỏ hàng bình thường và chế độ Mua ngay
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkout(string mode = "")
        {
            List<CartItem> cart;
            if (mode == "buynow")
            {
                cart = ApplicationContext.GetSessionData<List<CartItem>>("BUY_NOW_TEMP") ?? new List<CartItem>();
                ViewBag.IsBuyNow = true;
            }
            else
            {
                cart = CartHelper.GetCart(HttpContext);
                ViewBag.IsBuyNow = false;
            }

            if (cart.Count == 0)
            {
                TempData["Message"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            // Có thể lấy thông tin mặc định của khách hàng từ DB để điền sẵn vào form
            var userData = WebUserData.GetUserData(User);
            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserID));

            return View(customer);
        }

        /// <summary>
        /// Xử lý lưu đơn hàng vào Database khi nhấn XÁC NHẬN ĐẶT HÀNG
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress, bool isBuyNow = false)
        {
            // --- THAY ĐỔI LOGIC LẤY GIỎ HÀNG ---
            List<CartItem> cart;
            if (isBuyNow)
            {
                // Lấy từ Session tạm nếu là chế độ Mua ngay
                cart = ApplicationContext.GetSessionData<List<CartItem>>("BUY_NOW_TEMP") ?? new List<CartItem>();
            }
            else
            {
                // Lấy từ Cookie nếu là đặt hàng bình thường
                cart = CartHelper.GetCart(HttpContext);
            }
            // ----------------------------------

            if (cart == null || cart.Count == 0)
            {
                TempData["Message"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrEmpty(deliveryProvince) || string.IsNullOrEmpty(deliveryAddress))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin giao hàng.");
                // Trả về lại trang nhập liệu và cần nạp lại thông tin Customer cho View
                var userDataErr = WebUserData.GetUserData(User);
                var customerErr = await PartnerDataService.GetCustomerAsync(int.Parse(userDataErr.UserID));
                ViewBag.IsBuyNow = isBuyNow;
                return View(customerErr);
            }

            var userData = WebUserData.GetUserData(User);
            if (userData == null) return RedirectToAction("Login", "Account");

            // 1. Khởi tạo đơn hàng
            var orderData = new Order()
            {
                CustomerID = int.Parse(userData.UserID),
                OrderTime = DateTime.Now,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress,
                Status = OrderStatusEnum.New,
                EmployeeID = null
            };

            // 2. Lưu đơn hàng
            int orderID = await SalesDataService.AddOrderAsync(orderData);

            if (orderID > 0)
            {
                // 3. Lưu chi tiết đơn hàng
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

                // --- THAY ĐỔI LOGIC DỌN DẸP ---
                if (isBuyNow)
                {
                    // Xóa dữ liệu tạm trong Session
                    ApplicationContext.Current.Session.Remove("BUY_NOW_TEMP");
                }
                else
                {
                    // Dọn dẹp giỏ hàng chính trong Cookie
                    CartHelper.ClearCart(HttpContext);
                }
                // ------------------------------

                TempData["Message"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là #{orderID}";
                return RedirectToAction("Finish", new { id = orderID });
            }

            TempData["Message"] = "Không thể xử lý đơn hàng lúc này.";
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitOrder(string province, string address)
        {
            // 1. Lấy giỏ hàng từ Cookie thông qua CartHelper của bạn
            var cart = CartHelper.GetCart(HttpContext);
            if (cart.Count == 0)
                return Json(new { code = 0, message = "Giỏ hàng của bạn đang trống." });

            // 2. Kiểm tra đăng nhập để lấy CustomerID
            var customerIdStr = HttpContext.Session.GetString("CustomerID");
            if (string.IsNullOrEmpty(customerIdStr))
                return Json(new { code = 0, message = "Vui lòng đăng nhập trước khi đặt hàng." });

            int customerId = int.Parse(customerIdStr);

            try
            {
                // 3. Khởi tạo đối tượng đơn hàng (Bảng Orders)
                var order = new Order()
                {
                    CustomerID = customerId,
                    OrderTime = DateTime.Now,
                    DeliveryProvince = province,
                    DeliveryAddress = address,
                    Status = OrderStatusEnum.New, // Trạng thái: Chờ duyệt (hoặc theo Enum của bạn)
                    EmployeeID = null,
                    ShipperID = null
                };

                // 4. Lưu đơn hàng vào DB và lấy OrderID vừa sinh ra
                // Lưu ý: AddOrderAsync trong SalesDataService phải trả về int (ID vừa tạo)
                int orderID = await SalesDataService.AddOrderAsync(order);

                // 5. Lưu chi tiết đơn hàng (Bảng OrderDetails)
                foreach (var item in cart)
                {
                    var detail = new OrderDetail()
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice // Giá bán tại thời điểm khách đặt
                    };
                    await SalesDataService.AddDetailAsync(detail);
                }

                // 6. Đặt hàng thành công -> Xóa giỏ hàng trong Cookie
                CartHelper.ClearCart(HttpContext);

                // Trả về code = 1 để JavaScript bên View chuyển hướng
                return Json(new { code = 1, data = orderID });
            }
            catch (Exception ex)
            {
                return Json(new { code = 0, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Trang thông báo thành công
        public IActionResult Finish(int id)
        {
            ViewBag.OrderID = id;
            return View();
        }

        /// <summary>
        /// Lịch sử mua hàng
        /// </summary>
        public async Task<IActionResult> History(int page = 1, string searchValue = "")
        {
            var userData = WebUserData.GetUserData(User);
            int currentCustomerId = int.Parse(userData.UserID);

            var input = new OrderSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue ?? "",
                Status = 0
            };

            // Lấy toàn bộ đơn hàng (Cần lọc theo CustomerID)
            var result = await SalesDataService.ListOrdersAsync(input);

            // Lọc đơn hàng của chính khách hàng này
            var myOrders = result.DataItems
                           .Where(x => x.CustomerID == currentCustomerId)
                           .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.SearchValue = searchValue;

            return View(myOrders);
        }

        /// <summary>
        /// Khách hàng tự hủy đơn hàng khi đơn hàng đang ở trạng thái vừa tạo (New)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userData = WebUserData.GetUserData(User);
            var order = await SalesDataService.GetOrderAsync(id);

            // 1. Kiểm tra đơn hàng có tồn tại không
            if (order == null)
                return Json(new { code = 0, message = "Đơn hàng không tồn tại." });

            // 2. Bảo mật: Kiểm tra đơn hàng này có phải của khách hàng đang đăng nhập không
            if (order.CustomerID.ToString() != userData.UserID)
                return Json(new { code = 0, message = "Bạn không có quyền hủy đơn hàng này." });

            // 3. Nghiệp vụ: Chỉ cho phép hủy khi trạng thái là "Vừa tạo" (New)
            if (order.Status != OrderStatusEnum.New)
            {
                return Json(new
                {
                    code = 0,
                    message = "Đơn hàng đã được tiếp nhận xử lý, bạn không thể tự hủy lúc này. Vui lòng liên hệ shop để hỗ trợ."
                });
            }

            // 4. Gọi Service để thực hiện hủy
            bool result = await SalesDataService.CancelOrderAsync(id);

            if (result)
                return Json(new { code = 1, message = "Đã hủy đơn hàng thành công." });
            else
                return Json(new { code = 0, message = "Lỗi hệ thống khi thực hiện hủy đơn." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAddress(int orderId, string deliveryAddress, string deliveryProvince)
        {
            // Kiểm tra dữ liệu đầu vào cơ bản
            if (string.IsNullOrWhiteSpace(deliveryAddress) || string.IsNullOrWhiteSpace(deliveryProvince))
            {
                return Json(new { code = 0, message = "Vui lòng nhập đầy đủ địa chỉ và tỉnh thành." });
            }

            // Gọi Business Layer xử lý
            bool result = await SalesDataService.UpdateDeliveryInfoAsync(orderId, deliveryAddress, deliveryProvince);

            if (result)
            {
                return Json(new { code = 1, message = "Cập nhật địa chỉ giao hàng thành công." });
            }
            else
            {
                return Json(new { code = 0, message = "Không thể cập nhật địa chỉ. Đơn hàng không tồn tại hoặc trạng thái hiện tại không cho phép chỉnh sửa." });
            }
        }
        /// <summary>
        /// Chi tiết đơn hàng
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var userData = WebUserData.GetUserData(User);
            var order = await SalesDataService.GetOrderAsync(id);

            if (order == null || order.CustomerID.ToString() != userData.UserID)
            {
                return NotFound();
            }

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Order = order;
            return View(details);
        }
    }
}