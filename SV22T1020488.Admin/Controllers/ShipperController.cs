using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;

namespace SV22T1020488.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        private const int PAGESIZE = 10;
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm người giao hàng trong session
        /// </summary>
        private const string SHIPPER_SEARCH = "ShipperSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
            return View(result);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới đơn vị vận chuyển";
            var model = new Shipper() { ShipperID = 0 };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật 1 đơn vị vận chuyển
        /// </summary>
        /// <param name="id">Mã đơn vị vận chuyển cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật đơn vị vận chuyển";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            try
            {
                // Logic kiểm tra dữ liệu
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Tên đơn vị vận chuyển không được để trống");

                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.ShipperID == 0 ? "Thêm mới đơn vị vận chuyển" : "Cập nhật đơn vị vận chuyển";
                    return View("Edit", data);
                }

                if (data.ShipperID == 0)
                {
                    await PartnerDataService.AddShipperAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống. Vui lòng thử lại sau.");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa 1 đơn vị vận chuyển
        /// </summary>
        /// <param name="id">Mã đơn vị vận chuyển cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            // Trường hợp: Thực hiện xóa (Yêu cầu gửi lên bằng POST)
            if (HttpMethods.IsPost(Request.Method))
            {
                // Kiểm tra lại ràng buộc dữ liệu phía Server trước khi xóa thực sự
                if (await PartnerDataService.IsUsedShipperAsync(id))
                {
                    ModelState.AddModelError(string.Empty, "Không thể xóa đơn vị vận chuyển này vì đang có các đơn hàng liên quan.");
                    var modelErr = await PartnerDataService.GetShipperAsync(id);
                    ViewBag.CanDelete = false;
                    return View(modelErr);
                }

                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }

            // Trường hợp: Hiển thị giao diện xác nhận (Yêu cầu GET)
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra khả năng xóa để báo cho View ẩn/hiện nút bấm
            ViewBag.CanDelete = !await PartnerDataService.IsUsedShipperAsync(id);

            return View(model);
        }
    }
}
