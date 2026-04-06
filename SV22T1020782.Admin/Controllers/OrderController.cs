using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;
using SV22T1020782.Models.Catalog;
using SV22T1020782.Models.Sales;

namespace SV22T1020782.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class OrderController : Controller
    {
        public const string SEARCH_ORDER = "SearchOrder";
        public const string SEARCH_PRODUCT = "SearchProductForOrder";
        public const string DRAFT_ORDER = "DraftOrder";

        #region Các chuc nang tìm kiếm đon hàng
        // Order/Index
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(SEARCH_ORDER);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = 10,
                    SearchValue = "",
                    Status = null,
                    DateFrom = null,
                    DateTo = null
                };
            }

            return View(input);
        }

        // Order/Search
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            if (input.PageSize <= 0)
                input.PageSize = 10;

            var result = await SalesDataService.ListOrdersAsync(input);

            ApplicationContext.SetSessionData(SEARCH_ORDER, input);

            return View(result);
        }
        #endregion

        #region Các chức năng liên quan đến tạo đơn hàng moi
        // Order/Create
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = null,
                    MaxPrice = null,
                    SearchValue = ""
                };
            var draft = ApplicationContext.GetSessionData<Order>(DRAFT_ORDER);
            ViewBag.DraftOrder = draft;

            return View(input);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }
        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Bổ sung hàng vào giỏ
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        /// <returns>
        /// Chuỗi Json khác rỗng thông báo lỗi nếu không thành công,
        /// nguoc lại trả về chuỗi json rỗng
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal salePrice)
        {
            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Mat hàng không tồn tại"));

            // Kiểm tra số luong, giá bán
            // 1. Kiểm tra số lượng (phải lớn hơn 0)
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng mặt hàng được thêm phải lớn hơn 0."));
            }

            // 2. Kiểm tra giá bán (không được là số âm, có thể bằng 0 nếu là hàng tặng/khuyến mãi)
            if (salePrice < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ (không được nhỏ hơn 0)."));
            }

            var item = new OrderDetailViewInfo()
            {
                ProductID = productID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = string.IsNullOrEmpty(product.Photo) ? "nophoto.png" : product.Photo,
                Quantity = quantity,
                SalePrice = salePrice
            };

            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Cập nhật thông tin của một mat hàng trong giỏ hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            return PartialView(item);
        }

        /// <summary>
        /// Cập nhật mat hàng trong giỏ
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            // 1. Kiểm tra số lượng (phải lớn hơn 0)
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng mặt hàng phải lớn hơn 0. Nếu muốn xóa, vui lòng dùng nút Xóa."));
            }

            // 2. Kiểm tra giá bán (không được là số âm)
            if (salePrice < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ (không được nhỏ hơn 0)."));
            }

            ShoppingCartHelper.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }
        /// <summary>
        /// Xóa hàng trong giỏ hoặc trong chi tiết đơn hàng
        /// </summary>
        /// <param name="id">0: Hàng trong giỏ, khác 0: mã của đơn hàng</param>
        /// <param name="productId"></param>
        /// <returns></returns>
        // Order/DeleteCartItem/{id}?productId={productId}
        public IActionResult DeleteCartItem(int productId = 0)
        {
            //POST: Xoá khỏi giỏ
            if(Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
                return Json(new ApiResult(1, ""));
            }
            //GET: Hiển thị hộp thoại để xác nhận
            ViewBag.ProductID = productId;
            return PartialView();
        }
        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        /// <returns></returns>

        // Order/ClearCart
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }

            return PartialView();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customerID"></param>
        /// <param name="province"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartHelper.GetShoppingCart();

            // 1. Kiểm tra tính hợp lệ của dữ liệu
            if (cart.Count == 0) return Json(new ApiResult(0, "Giỏ hàng đang trống"));
            if (customerID == 0) return Json(new ApiResult(0, "Vui lòng chọn khách hàng"));
            if (string.IsNullOrWhiteSpace(province)) return Json(new ApiResult(0, "Vui lòng chọn Tỉnh/Thành phố"));
            if (string.IsNullOrWhiteSpace(address)) return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng"));

            // 2. Gọi hàm tạo đơn hàng (KHÔNG truyền employeeID)
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            int orderID = await SalesDataService.AddOrderAsync(customerID, province, address, cart);

            if (orderID > 0)
            {
                // Thành công: Xóa giỏ hàng và báo về View để chuyển trang
                ShoppingCartHelper.ClearCart();
                ApplicationContext.SetSessionData(DRAFT_ORDER, null!);

                return Json(new ApiResult(orderID, ""));
            }
            else
            {
                // Thất bại
                return Json(new ApiResult(0, "Lập đơn hàng thất bại. Vui lòng thử lại sau."));
            }
        }

        [HttpPost]
        public IActionResult SaveDraftOrder(int customerID = 0, string province = "", string address = "")
        {
            // Tạo 1 object Order tạm để lưu thông tin nháp
            var draft = new Order
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address
            };
            ApplicationContext.SetSessionData(DRAFT_ORDER, draft);
            return Json(new { success = true });
        }

        #endregion

        #region Các chuc nang xem và xu lý đon hàng
        // 1. Chi tiết đơn hàng
        public async Task<IActionResult> Detail(int id = 0)
        {
            // 1. Lấy thông tin đơn hàng
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                return RedirectToAction("Index"); // Nấu không thấy đơn hàng thì quay về danh sách
            }

            // 2. Lấy danh sách mặt hàng thuộc đơn hàng này
            var details = await SalesDataService.ListDetailsAsync(id);

            // Truyền chi tiết qua ViewBag, còn thông tin đơn truyền bằng Model
            ViewBag.Details = details;

            return View(order);
        }

        // 2. Duyệt đơn hàng
        [HttpGet]
        public IActionResult Accept(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Accept(int id, IFormCollection form)
        {
            // Lấy ID của nhân viên đang đăng nhập ("Lê Đức Dũng")
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            await SalesDataService.AcceptOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id = id });
        }

        // 3. Chuyển người giao hàng
        [HttpGet]
        public IActionResult Shipping(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            if (shipperID <= 0)
            {
                TempData["Message"] = "Vui lòng chọn người giao hàng!";
                return RedirectToAction("Detail", new { id = id });
            }
            await SalesDataService.ShipOrderAsync(id, shipperID);
            return RedirectToAction("Detail", new { id = id });
        }

        // 4. Hoàn tất đơn hàng
        [HttpGet]
        public IActionResult Finish(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Finish(int id, IFormCollection form)
        {
            await SalesDataService.CompleteOrderAsync(id);
            return RedirectToAction("Detail", new { id = id });
        }

        // 5. Hủy đơn hàng
        [HttpGet]
        public IActionResult Cancel(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, IFormCollection form)
        {
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            // Đã thêm truyền employeeID vào hàm Cancel
            await SalesDataService.CancelOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id = id });
        }

        // 6. Từ chối đơn hàng
        [HttpGet]
        public IActionResult Reject(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Reject(int id, IFormCollection form)
        {
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            await SalesDataService.RejectOrderAsync(id, employeeID);
            return RedirectToAction("Detail", new { id = id });
        }

        // 7. Xóa đơn hàng
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id, IFormCollection form)
        {
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (result)
                return RedirectToAction("Index"); // Xóa thành công thì đá về trang danh sách

            return RedirectToAction("Detail", new { id = id });
        }

        // 1. Hiển thị Popup sửa mặt hàng trong đơn
        [HttpGet]
        public async Task<IActionResult> EditDetail(int id = 0, int productId = 0)
        {
            // Lấy thông tin chi tiết của mặt hàng đó để hiển thị lên form
            var detail = await SalesDataService.GetDetailAsync(id, productId);
            if (detail == null)
            {
                return Content("Không tìm thấy thông tin mặt hàng này!");
            }
            return PartialView(detail);
        }

        // 2. Xử lý khi bấm nút "Lưu thay đổi" trên Popup sửa
        [HttpPost]
        public async Task<IActionResult> UpdateDetail(int orderID, int productID, int quantity, decimal salePrice)
        {
            // Cập nhật lại vào DB
            var data = new OrderDetail()
            {
                OrderID = orderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = salePrice
            };
            await SalesDataService.UpdateDetailAsync(data);

            // Xong thì tải lại trang Chi tiết đơn hàng
            return RedirectToAction("Detail", new { id = orderID });
        }

        // 3. Hiển thị Popup hỏi xác nhận xóa mặt hàng khỏi đơn
        [HttpGet]
        public IActionResult DeleteDetail(int id = 0, int productId = 0)
        {
            ViewBag.OrderID = id;
            ViewBag.ProductID = productId;
            return PartialView();
        }

        // 4. Xử lý khi bấm "Xác nhận xóa"
        [HttpPost]
        [ActionName("DeleteDetail")]
        public async Task<IActionResult> ConfirmDeleteDetail(int orderID, int productID)
        {
            // Xóa khỏi DB
            await SalesDataService.DeleteDetailAsync(orderID, productID);

            // Tải lại trang Chi tiết
            return RedirectToAction("Detail", new { id = orderID });
        }
        #endregion
    }
}
