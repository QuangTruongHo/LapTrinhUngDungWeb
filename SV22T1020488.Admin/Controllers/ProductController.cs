using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020488.BusinessLayers;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;

namespace SV22T1020488.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý các hoạt động liên quan đến mặt hàng (sản phẩm)
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ProductController : Controller
    {
        private const int PAGESIZE = 10;
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            return View(input);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            input.SearchValue ??= "";
            if (input.MinPrice < 0) input.MinPrice = 0;
            if (input.MaxPrice < 0) input.MaxPrice = 0;
            if (input.MaxPrice > 0 && input.MaxPrice < input.MinPrice) input.MaxPrice = input.MinPrice;

            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return PartialView(result);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới mặt hàng";
            var model = new Product() { ProductID = 0, Photo = "", IsSelling = true };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật mặt hàng";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null) return RedirectToAction("Index");

            // Đảm bảo ViewBag.ProductID luôn có giá trị cho các Partial View
            ViewBag.ProductID = id;
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");
                if (data.CategoryID == 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");
                if (data.SupplierID == 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");
                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.ProductID == 0 ? "Thêm mới mặt hàng" : "Cập nhật mặt hàng";
                    return View("Edit", data);
                }

                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string folder = Path.Combine(ApplicationContext.WWWRootPath, "images", "products");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string filePath = Path.Combine(folder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi hệ thống: " + ex.Message);
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (HttpMethods.IsPost(Request.Method))
            {
                await CatalogDataService.DeleteProductAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.CanDelete = !await CatalogDataService.IsUsedProductAsync(id);
            return View(model);
        }

        // --- CÁC HÀM XỬ LÝ ẢNH (PHOTOS) ---

        public async Task<IActionResult> CreatePhoto(int id, string method, long photoId = 0)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            ViewBag.ProductID = id;
            ViewBag.ProductName = product.ProductName;

            switch (method.ToLower())
            {
                case "add":
                    ViewBag.Title = "Thêm ảnh cho mặt hàng";
                    var newPhoto = new ProductPhoto { ProductID = id, DisplayOrder = 1, IsHidden = false };
                    return View("CreatePhoto", newPhoto);
                case "edit":
                    ViewBag.Title = "Thay đổi ảnh mặt hàng";
                    var editPhoto = await CatalogDataService.GetPhotoAsync(photoId);
                    if (editPhoto == null) return RedirectToAction("Edit", new { id = id });
                    return View("CreatePhoto", editPhoto);
                case "delete":
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    return RedirectToAction("Edit", new { id = id });
                default:
                    return RedirectToAction("Edit", new { id = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            if (string.IsNullOrWhiteSpace(data.Description))
                ModelState.AddModelError(nameof(data.Description), "Mô tả không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.PhotoID == 0 ? "Thêm ảnh cho mặt hàng" : "Thay đổi ảnh mặt hàng";
                return View("CreatePhoto", data);
            }

            if (uploadPhoto != null && uploadPhoto.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await uploadPhoto.CopyToAsync(ms);
                    byte[] fileBytes = ms.ToArray();
                    string contentType = uploadPhoto.ContentType;
                    data.Photo = $"data:{contentType};base64,{Convert.ToBase64String(fileBytes)}";
                }
            }
            else if (data.PhotoID > 0 && string.IsNullOrEmpty(data.Photo))
            {
                var oldPhoto = await CatalogDataService.GetPhotoAsync(data.PhotoID);
                data.Photo = oldPhoto?.Photo ?? "";
            }

            if (data.PhotoID == 0)
                await CatalogDataService.AddPhotoAsync(data);
            else
                await CatalogDataService.UpdatePhotoAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        // --- CÁC HÀM XỬ LÝ THUỘC TÍNH (ATTRIBUTES) ---

        [HttpGet]
        public async Task<IActionResult> CreateAttributes(int id, string method, long attributeId = 0)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            ViewBag.ProductID = id;
            ViewBag.ProductName = product.ProductName;

            switch (method.ToLower())
            {
                case "add":
                    ViewBag.Title = "Thêm thuộc tính mặt hàng";
                    var newAttr = new ProductAttribute { ProductID = id, DisplayOrder = 1 };
                    // Sửa từ "CreateAttribute" thành "CreateAttributes" cho đúng ý bạn
                    return View("CreateAttributes", newAttr);
                case "edit":
                    ViewBag.Title = "Chỉnh sửa thuộc tính mặt hàng";
                    var editAttr = await CatalogDataService.GetAttributeAsync(attributeId);
                    if (editAttr == null) return RedirectToAction("Edit", new { id = id });
                    // Sửa từ "CreateAttribute" thành "CreateAttributes"
                    return View("CreateAttributes", editAttr);
                case "delete":
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    return RedirectToAction("Edit", new { id = id });
                default:
                    return RedirectToAction("Edit", new { id = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");
            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.AttributeID == 0 ? "Thêm thuộc tính mặt hàng" : "Chỉnh sửa thuộc tính mặt hàng";
                // Sửa từ "CreateAttribute" thành "CreateAttributes"
                return View("CreateAttributes", data);
            }

            if (data.AttributeID == 0)
                await CatalogDataService.AddAttributeAsync(data);
            else
                await CatalogDataService.UpdateAttributeAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }
    }
}