using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;
using SV22T1020782.Models.Catalog;
using SV22T1020782.Models.Common;

namespace SV22T1020782.Shop.Controllers
{
    public class ProductController : Controller
    {
        // Tái sử dụng lại biến Session lưu trạng thái tìm kiếm
        public const string SEARCH_SHOP_PRODUCT = "SearchShopProduct";

        /// <summary>
        /// Giao diện trang Danh sách Sản phẩm
        /// </summary>
        public async Task<IActionResult> Index(string searchValue = "", int categoryId = 0, bool reset = false)
        {
            if (reset)
            {
                ApplicationContext.SetSessionData(SEARCH_SHOP_PRODUCT, null!);
            }

            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_SHOP_PRODUCT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 12,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }

            // Nếu có từ khóa từ thanh tìm kiếm
            if (Request.Query.ContainsKey("searchValue"))
            {
                input.SearchValue = searchValue ?? "";
                input.CategoryID = 0;
                input.MinPrice = 0;
                input.MaxPrice = 0;
                input.Page = 1;
            }

            // BỔ SUNG LOGIC: Khi khách hàng bấm Loại hàng từ Trang chủ truyền sang
            if (categoryId > 0)
            {
                input.CategoryID = categoryId;
                input.SearchValue = ""; // Xóa từ khóa tìm kiếm cũ nếu có
                input.MinPrice = 0;
                input.MaxPrice = 0;
                input.Page = 1; // Reset về trang 1
            }

            ApplicationContext.SetSessionData(SEARCH_SHOP_PRODUCT, input);

            // Lấy danh sách Loại hàng để làm Sidebar bên trái
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 100, SearchValue = "" });
            ViewBag.Categories = categoryResult.DataItems;

            return View(input);
        }

        /// <summary>
        /// Gọi AJAX để lấy danh sách thẻ sản phẩm
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            if (input.PageSize <= 0) input.PageSize = 12;

            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_SHOP_PRODUCT, input);

            // CHÚ Ý: Trả về PartialView "_Search" (nó sẽ tự tìm trong thư mục Views/Product)
            return PartialView("_Search", result);
        }

        /// <summary>
        /// Yêu cầu 5: Xem chi tiết mặt hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id = 0)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index"); // Không thấy thì quay về trang chủ
            }

            // Lấy thêm danh sách ảnh và thuộc tính của sản phẩm
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }
    }
}