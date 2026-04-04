using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;
using SV22T1020782.Models.Common;
using SV22T1020782.Shop.Models;
using System.Diagnostics;

namespace SV22T1020782.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Trang chủ (Landing Page)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Chỉ cần lấy danh sách Loại hàng để làm các nút "Khám phá danh mục"
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 100, SearchValue = "" });
            ViewBag.Categories = categoryResult.DataItems;

            return View(); // Không cần truyền Model (ProductSearchInput) sang nữa
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}