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
        /// <summary>
        /// Giao diện danh sách đơn hàng
        /// </summary>
        /// 
        private const int PAGESIZE = 10;
        private const string PRODUCT_SEARCH = "ProductSearchInput";

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
                    Status = 0, // Giả sử 0 là "Tất cả" trong Enum của bạn
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
                    input.DateFrom = DateTime.ParseExact(dates[0], "d/m/yyyy", CultureInfo.InvariantCulture);
                    input.DateTo = DateTime.ParseExact(dates[1], "d/m/yyyy", CultureInfo.InvariantCulture);
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
        /// Giao diện lập đơn hàng mới
        /// </summary>
        
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);

        }

        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }

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
        /// <param name="id">Mã đơn hàng</param>
        public async Task<IActionResult> Detail(int id)
        {
            // 1. Lấy thông tin đơn hàng (trả về kiểu OrderViewInfo đã có đủ tên khách, nhân viên, shipper)
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            // 2. Lấy danh sách mặt hàng của đơn hàng này
            var details = await SalesDataService.ListDetailsAsync(id);

            // 3. Truyền dữ liệu sang View thông qua một Tuple hoặc ViewModel
            // Ở đây tôi dùng Tuple cho nhanh: Item1 là Order, Item2 là List Details
            var model = new Tuple<OrderViewInfo, List<OrderDetailViewInfo>>(order, details);

            return View(model);
        }

        /// <summary>
        /// Hiển thị thông tin của mặt hàng cần cập nhật
        /// </summary>

        /// <param name="productId">Mã sản phẩm</param>
        public IActionResult EditCartItem (int productId=0)
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
        /// <summary>
        /// Xóa một sản phẩm khỏi danh sách mặt hàng của đơn hàng
        /// </summary>
        /// <param name="productId">Mã sản phẩm</param>
        public IActionResult DeleteCartItem(int productId=0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1));
            }

            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        /// <summary>
        /// Xóa toàn bộ sản phẩm đã chọn trong giỏ hàng
        /// </summary>
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1));
            }
                return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
                return Json(new ApiResult(0, "Giỏ hàng trống"));

            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address

            };
            int orderID = SalesDataService.AddOrderAsync(order).Result;
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
            //Trả về kết quả thành công với code là mã đơn hàng mới
            return Json(new ApiResult(orderID));
        }
        /// <summary>
        /// Chấp nhận/Duyệt đơn hàng (Chuyển từ trạng thái Chờ sang Đã xác nhận)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        public async Task<IActionResult> Accept(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                if (order.Status != OrderStatusEnum.New)
                    return Json(new { code = 0, message = "Chỉ đơn hàng mới mới được phép duyệt." });

                bool result = await SalesDataService.AcceptOrderAsync(id, 1); // ID nhân viên tạm thời là 1
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi duyệt đơn." });
            }
            return View(order);
        }


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
            return View(order);
        }

        /// <summary>
        /// Xác nhận đơn hàng đã giao thành công và kết thúc quy trình
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        public async Task<IActionResult> Finish(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                if ((int)order.Status != 3)
                    return Json(new { code = 0, message = "Đơn hàng phải ở trạng thái đang giao mới có thể hoàn tất" });

                bool result = await SalesDataService.CompleteOrderAsync(id);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi hoàn tất đơn hàng" });
            }
            return View(order);
        }

        /// <summary>
        /// Từ chối đơn hàng (Trường hợp đơn hàng không hợp lệ ngay từ đầu)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        public async Task<IActionResult> Reject(int id)
        {
            if (Request.Method == "POST")
            {
                bool result = await SalesDataService.RejectOrderAsync(id, 1);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi từ chối" });
            }
            var data = await SalesDataService.GetOrderAsync(id);
            return View(data);
        }

        /// <summary>
        /// Hủy đơn hàng (Trường hợp khách yêu cầu hủy khi đơn đã được duyệt hoặc đang giao)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>


        public async Task<IActionResult> Cancel(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                // Không cho phép hủy nếu đã hoàn tất
                if (order.Status == OrderStatusEnum.Completed)
                    return Json(new { code = 0, message = "Đơn hàng đã hoàn tất, không thể hủy." });

                bool result = await SalesDataService.CancelOrderAsync(id);
                return Json(result ? new { code = 1 } : new { code = 0, message = "Lỗi khi hủy đơn." });
            }
            return View(order);
        }

        /// <summary>
        /// Xóa vĩnh viễn đơn hàng khỏi hệ thống
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        public async Task<IActionResult> Delete(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return NotFound();

            if (Request.Method == "POST")
            {
                // Chỉ cho phép xóa nếu là Mới, Đã Hủy hoặc Bị Từ Chối
                if (order.Status == OrderStatusEnum.New ||
                    order.Status == OrderStatusEnum.Cancelled ||
                    order.Status == OrderStatusEnum.Rejected)
                {
                    bool result = await SalesDataService.DeleteOrderAsync(id);
                    return Json(result ? new { code = 1, url = "/Order" } : new { code = 0, message = "Lỗi khi xóa." });
                }
                return Json(new { code = 0, message = "Không được phép xóa đơn hàng đang xử lý." });
            }
            return View(order);
        }
    }
}
