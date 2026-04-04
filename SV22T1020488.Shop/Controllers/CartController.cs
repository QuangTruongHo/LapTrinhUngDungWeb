using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Shop.Models;
using System.Linq;

namespace SV22T1020488.Shop.Controllers
{
    public class CartController : Controller
    {
        /// <summary>
        /// Trang hiển thị danh sách giỏ hàng
        /// </summary>
        public IActionResult Index()
        {
            var cart = CartHelper.GetCart(HttpContext);
            return View(cart);
        }

        /// <summary>
        /// Xử lý thêm sản phẩm vào giỏ hàng (Hỗ trợ Ajax hiệu ứng bay và Form truyền thống)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                if (quantity <= 0) quantity = 1;

                // Lấy thông tin sản phẩm từ Data Service
                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                {
                    return HandleResponse(false, "Sản phẩm không tồn tại", "Index", "Product");
                }

                // Lấy giỏ hàng hiện tại từ Session
                var cart = CartHelper.GetCart(HttpContext);
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
                        ProductName = product.ProductName ?? "",
                        Photo = product.Photo ?? "no-image.png",
                        Unit = product.Unit ?? "",
                        SalePrice = product.Price,
                        Quantity = quantity
                    });
                }

                // Lưu lại giỏ hàng vào Session
                CartHelper.SaveCart(HttpContext, cart);

                return HandleResponse(true, "Đã thêm vào giỏ hàng", "Index");
            }
            catch (Exception ex)
            {
                // Log lỗi ở đây nếu cần
                return HandleResponse(false, "Có lỗi xảy ra khi thêm vào giỏ hàng", "Index", "Home");
            }
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

        /// <summary>
        /// Hàm bổ trợ tự động nhận diện Ajax hoặc Redirect tùy theo yêu cầu từ Client
        /// </summary>
        private IActionResult HandleResponse(bool success, string message, string action, string? controller = null)
        {
            // Kiểm tra xem request có phải là Ajax không (dựa trên Header X-Requested-With)
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
            {
                var cart = CartHelper.GetCart(HttpContext);
                return Json(new
                {
                    success = success,
                    message = message,
                    totalItems = cart.Sum(x => x.Quantity)
                });
            }

            // Nếu không phải Ajax, chuyển hướng trang theo cách truyền thống
            return controller == null ? RedirectToAction(action) : RedirectToAction(action, controller);
        }
    }
}