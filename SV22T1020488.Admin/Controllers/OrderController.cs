using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Partner;
using SV22T1020488.Models.Sales;
using System.Buffers;
using System.Globalization;
using System.Threading.Tasks;

namespace SV22T1020488.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    /// <summary>
    /// Controller quản lý các nghiệp vụ liên quan đến đơn hàng
    /// </summary>
    public class OrderController : Controller
    {
        private const int PAGESIZE = 10;
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        /// <summary>
        /// Giao diện danh sách đơn hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0, // 0 đại diện cho trạng thái "Tất cả"
                    DateFrom = null,
                    DateTo = null
                };
            }
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm, lọc và phân trang đơn hàng
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input, string dateRange)
        {
            // 1. Xử lý khoảng ngày từ chuỗi "dd/MM/yyyy - dd/MM/yyyy"
            if (!string.IsNullOrEmpty(dateRange))
            {
                var dates = dateRange.Split(" - ");
                if (dates.Length == 2)
                {
                    input.DateFrom = DateTime.ParseExact(dates[0], "d/M/yyyy", CultureInfo.InvariantCulture);
                    input.DateTo = DateTime.ParseExact(dates[1], "d/M/yyyy", CultureInfo.InvariantCulture);
                }
            }

            input.SearchValue ??= "";

            // 2. Gọi Service thực tế từ SalesDataService
            var result = await SalesDataService.ListOrdersAsync(input);

            // 3. Lưu lại điều kiện tìm kiếm vào Session
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return PartialView(result);
        }

        /// <summary>
        /// Tìm kiếm mặt hàng để thêm vào đơn hàng
        /// </summary>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng hiện tại
        /// </summary>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hàng
        /// </summary>
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal price)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (price < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng đã ngừng bán"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = productID,
                Quantity = quantity,
                SalePrice = price,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png"
            };
            ShoppingCartService.AddCartItem(item);

            return Json(new ApiResult(1));
        }

        /// <summary>
        /// Giao diện lập đơn hàng mới
        /// </summary>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                };
            }
            return View(input);
        }

        /// <summary>
        /// Hiển thị thông tin chi tiết của một đơn hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            var details = await SalesDataService.ListDetailsAsync(id);
            var model = new Tuple<OrderViewInfo, List<OrderDetailViewInfo>>(order, details);

            return View(model);
        }

        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá không hợp lệ"));

            ShoppingCartService.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1));
        }

        public IActionResult DeleteCartItem(int productId = 0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1));
            }

            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1));
            }
            return PartialView();
        }

        /// <summary>
        /// Thực hiện lưu đơn hàng vào CSDL
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0) return Json(new ApiResult(0, "Giỏ hàng trống"));

            var userData = User.GetUserData();
            int employeeId = userData != null ? int.Parse(userData.UserId ?? "0") : 0;

            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address,
                EmployeeID = employeeId,
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New // Sử dụng Enum New = 1
            };

            int orderID = await SalesDataService.AddOrderAsync(order);

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

            ShoppingCartService.ClearCart();
            return Json(new ApiResult(orderID));
        }

        /// <summary>
        /// Chấp nhận/Duyệt đơn hàng
        /// </summary>
        public async Task<IActionResult> Accept(int id)
        {
            if (Request.Method == "POST")
            {
                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return Json(new { code = 0, message = "Đơn hàng không tồn tại." });

                if (order.Status != OrderStatusEnum.New)
                    return Json(new { code = 0, message = "Chỉ đơn hàng mới mới được phép duyệt." });

                var userData = User.GetUserData();
                int employeeId = userData != null ? int.Parse(userData.UserId ?? "0") : 0;

                bool result = await SalesDataService.AcceptOrderAsync(id, employeeId);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi duyệt đơn." });
            }

            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null) return NotFound();
            return PartialView(model);
        }

        /// <summary>
        /// Chuyển đơn hàng sang trạng thái đang giao
        /// </summary>
        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                if (order.Status != OrderStatusEnum.Accepted)
                    return Json(new { code = 0, message = "Đơn hàng phải được duyệt trước khi giao." });

                bool result = await SalesDataService.ShipOrderAsync(id, shipperID);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi giao hàng." });
            }
            return PartialView(order);
        }

        /// <summary>
        /// Xác nhận hoàn tất đơn hàng
        /// </summary>
        public async Task<IActionResult> Finish(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                // Sửa logic: Đã thay đổi ép kiểu (int)3 sang OrderStatusEnum.Shipping
                if (order.Status != OrderStatusEnum.Shipping)
                    return Json(new { code = 0, message = "Đơn hàng phải ở trạng thái đang giao mới có thể hoàn tất" });

                bool result = await SalesDataService.CompleteOrderAsync(id);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi hoàn tất đơn hàng" });
            }
            return PartialView(order);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public async Task<IActionResult> Reject(int id)
        {
            if (Request.Method == "POST")
            {
                var userData = User.GetUserData();
                int employeeId = userData != null ? int.Parse(userData.UserId ?? "0") : 0;

                bool result = await SalesDataService.RejectOrderAsync(id, employeeId);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi từ chối đơn hàng." });
            }

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();
            return PartialView(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                // Sửa logic: Kiểm tra trạng thái bằng Enum thay vì so sánh chuỗi
                if (order.Status == OrderStatusEnum.Completed)
                    return Json(new { code = 0, message = "Đơn hàng đã hoàn tất, không thể hủy." });

                bool result = await SalesDataService.CancelOrderAsync(id);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi hủy đơn." });
            }
            return PartialView(order);
        }

        /// <summary>
        /// Xóa đơn hàng (chỉ dành cho đơn hàng ở trạng thái kết thúc hoặc vừa tạo)
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                // Logic kiểm tra trạng thái cho phép xóa đồng bộ với Enum
                if (order.Status == OrderStatusEnum.New ||
                    order.Status == OrderStatusEnum.Cancelled ||
                    order.Status == OrderStatusEnum.Rejected)
                {
                    bool result = await SalesDataService.DeleteOrderAsync(id);
                    return Json(result ? new { code = 1, url = "/Order" } : new { code = 0, message = "Lỗi khi xóa." });
                }
                return Json(new { code = 0, message = "Không được phép xóa đơn hàng đang xử lý." });
            }
            return PartialView(order);
        }
    }
}