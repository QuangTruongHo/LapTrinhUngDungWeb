using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Shop.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020488.Shop.Controllers
{
    public class CartController : Controller
    {
        /// <summary>
        /// Trang hiển thị danh sách giỏ hàng (Index)
        /// </summary>
        public IActionResult Index()
        {
            var cart = CartHelper.GetCart(HttpContext);
            return View(cart);
        }

        /// <summary>
        /// Xử lý thêm sản phẩm vào giỏ hàng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                if (quantity <= 0) quantity = 1;

                // 1. Lấy thông tin sản phẩm từ Database
                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                {
                    return HandleResponse(false, "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.", "Index", "Home");
                }

                // 2. Lấy giỏ hàng từ Session
                var cart = CartHelper.GetCart(HttpContext);

                // 3. Kiểm tra sản phẩm đã có trong giỏ chưa
                var item = cart.FirstOrDefault(x => x.ProductID == id);

                if (item != null)
                {
                    item.Quantity += quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName ?? "Sản phẩm không tên",
                        Photo = product.Photo ?? "no-image.png",
                        Unit = product.Unit ?? "Cái",
                        SalePrice = product.Price,
                        Quantity = quantity
                    });
                }

                // 4. Lưu lại giỏ hàng vào Session
                CartHelper.SaveCart(HttpContext, cart);

                return HandleResponse(true, "Đã thêm sản phẩm vào giỏ hàng thành công!", "Index");
            }
            catch (Exception ex)
            {
                return HandleResponse(false, "Có lỗi xảy ra: " + ex.Message, "Index", "Home");
            }
        }

        /// <summary>
        /// CẬP NHẬT SỐ LƯỢNG (Dùng cho Ajax tăng giảm số lượng ở trang Cart)
        /// </summary>
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity, bool isBuyNow = false)
        {
            try
            {
                if (quantity <= 0)
                    return Json(new { success = false, message = "Số lượng không hợp lệ" });

                List<CartItem> cart;

                // Kiểm tra xem đang sửa ở giỏ hàng chính hay hàng "Mua ngay"
                if (isBuyNow)
                {
                    cart = ApplicationContext.GetSessionData<List<CartItem>>("BUY_NOW_TEMP") ?? new List<CartItem>();
                }
                else
                {
                    cart = CartHelper.GetCart(HttpContext);
                }

                var item = cart.FirstOrDefault(x => x.ProductID == id);
                if (item != null)
                {
                    item.Quantity = quantity;

                    // Lưu lại đúng vào Session tương ứng
                    if (isBuyNow)
                        ApplicationContext.SetSessionData("BUY_NOW_TEMP", cart);
                    else
                        CartHelper.SaveCart(HttpContext, cart);

                    return Json(new
                    {
                        success = true,
                        itemTotal = item.TotalPrice.ToString("N0"),
                        grandTotal = cart.Sum(x => x.TotalPrice).ToString("N0")
                    });
                }
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartAjax(int id, int quantity = 1)
        {
            return await AddToCart(id, quantity);
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi giỏ
        /// </summary>
        public IActionResult Remove(int id)
        {
            var cart = CartHelper.GetCart(HttpContext);
            var item = cart.FirstOrDefault(x => x.ProductID == id);

            if (item != null)
            {
                cart.Remove(item);
                CartHelper.SaveCart(HttpContext, cart);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Làm trống toàn bộ giỏ hàng
        /// </summary>
        public IActionResult Clear()
        {
            CartHelper.ClearCart(HttpContext);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> BuyNow(int id, int quantity = 1)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index", "Home");

            var buyNowItem = new CartItem
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Photo = product.Photo,
                Unit = product.Unit,
                SalePrice = product.Price,
                Quantity = quantity > 0 ? quantity : 1
            };

            // Sử dụng hàm có sẵn trong file ApplicationContext của bạn
            var tempCart = new List<CartItem> { buyNowItem };
            ApplicationContext.SetSessionData("BUY_NOW_TEMP", tempCart);

            return RedirectToAction("Checkout", "Order", new { mode = "buynow" });
        }

        /// <summary>
        /// Hàm xử lý phản hồi thông minh: Ajax trả về Json, Form trả về Redirect
        /// </summary>
        private IActionResult HandleResponse(bool success, string message, string action, string? controller = null)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Headers["Accept"].ToString().Contains("application/json");

            if (isAjax)
            {
                var cart = CartHelper.GetCart(HttpContext);
                return Json(new
                {
                    success = success,
                    message = message,
                    totalItems = cart.Count()
                });
            }

            if (controller != null)
                return RedirectToAction(action, controller);

            return RedirectToAction(action);
        }
    }
}