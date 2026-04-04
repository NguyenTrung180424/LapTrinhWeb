using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;
using SV22T1020782.Models.Sales;

namespace SV22T1020782.Shop.Controllers
{
    public class CartController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Add(int productID, int quantity = 1)
        {
            // CHẶN BƯỚC 1: Không cho thêm số lượng <= 0
            if (quantity <= 0)
                return Json(new { code = 0, message = "Số lượng mặt hàng phải lớn hơn 0!" });

            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Sản phẩm không tồn tại"));

            var item = new OrderDetailViewInfo()
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = product.Price // Lấy giá hiện tại của sản phẩm
            };

            ShoppingCartHelper.AddItemToCart(item); // Sử dụng Helper bạn đã copy sang

            // Trả về số lượng mặt hàng trong giỏ để cập nhật icon trên Header
            int cartCount = ShoppingCartHelper.GetShoppingCart().Count;
            return Json(new { code = 1, message = "Đã thêm vào giỏ hàng", count = cartCount });
        }

        /// <summary>
        /// Yêu cầu 7: Hiển thị giỏ hàng
        /// </summary>
        public IActionResult Index()
        {
            var cart = ShoppingCartHelper.GetShoppingCart(); //
            return View(cart);
        }

        /// <summary>
        /// Yêu cầu 7: Cập nhật số lượng
        /// </summary>
        [HttpPost]
        public IActionResult Update(int id, int quantity)
        {
            // CHẶN BƯỚC 2: Cập nhật giỏ hàng không cho phép <= 0
            if (quantity <= 0)
            {
                // Ghi lại thông báo lỗi để hiển thị lên View
                TempData["ErrorMessage"] = "Số lượng không hợp lệ. Vui lòng dùng nút Xóa nếu muốn bỏ mặt hàng này.";
                return RedirectToAction("Index");
            }

            var item = ShoppingCartHelper.GetCartItem(id);
            if (item != null)
            {
                ShoppingCartHelper.UpdateCartItem(id, quantity, item.SalePrice);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Yêu cầu 7: Xóa 1 mặt hàng
        /// </summary>
        public IActionResult Remove(int id)
        {
            ShoppingCartHelper.RemoveItemFromCart(id); //
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Yêu cầu 7: Xóa sạch giỏ hàng
        /// </summary>
        public IActionResult Clear()
        {
            ShoppingCartHelper.ClearCart(); //
            return RedirectToAction("Index");
        }

    }
}