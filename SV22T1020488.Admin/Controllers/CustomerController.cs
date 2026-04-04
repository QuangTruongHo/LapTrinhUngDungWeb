using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SV22T1020488.Admin;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace SV22T1020488.Admin.Controllers
{

    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class CustomerController : Controller
    {
        private const int PAGESIZE = 10;
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm khách hàng trong session
        /// </summary>
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
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
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Tạo mới 1 khách hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {

            ViewBag.Title = "Thêm mới khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật 1 khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";
                //TODO: Kiểm tra dữ liệu có hợp lệ không

                //Cách làm: Sử dụng ModelState dể lưu các tình huống lỗi và thông báo lỗi cho người dùng(trên View)
                //Giả định: Chỉ yêu cầu nhập tên, emai, tỉnh/thành
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError("CustomerName", "Vui lòng nhập tên khách hàng");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không được bỏ trống");
                else if (await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID))
                    ModelState.AddModelError(nameof(data.Email), "Email bị trùng");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //(Tùy chọn) Hiệu chỉnh dữ liệu theo qui định cuả hệ thống
                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";


                //Lưu dữ liệu vào CSDL
                if (data.CustomerID == 0)
                    await PartnerDataService.AddCustomerAsync(data);
                else
                    await PartnerDataService.UpdateCustomerAsync(data);
                return RedirectToAction("Index");
                
            }
            catch (Exception ex)
            {
                //Ghi log lỗi dựa vào thông tin Exception (ex.Message, ex.StackTrace)
                ModelState.AddModelError("Error", "Hệ thống đang bận, vui lòng thử lại sau ít phút");
                return View("Edit", data);
            }
            
        }
        /// <summary>
        /// Xóa 1 khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            //Nếu method bằng POST thì xóa
            if(Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            //GET Hiển thị thông tin khách hàng cần xóa
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.CanDelete = !await PartnerDataService.IsUsedCustomerAsync(id);

            return View(model);
        }

        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần đổi mật khẩu </param>
        /// <returns></returns>
        public IActionResult ChangePassword(int id)
        {
            return View();
        }
    }
}