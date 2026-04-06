using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;
using SV22T1020782.Models.Sales;

namespace SV22T1020782.Shop.Controllers
{
    [Authorize] // 🔐 Chốt chặn: Bắt buộc đăng nhập mới được đặt hàng!
    public class OrderController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            // 1. Kiểm tra giỏ hàng
            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart"); // Giỏ trống thì đá về trang Giỏ hàng
            }

            // 2. Lấy thông tin khách hàng để điền sẵn vào Form địa chỉ
            var userData = User.GetUserData();

            // BỔ SUNG LỚP BẢO VỆ: Kiểm tra null để diệt Warning
            if (userData == null || string.IsNullOrEmpty(userData.Email))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lúc này userData.Email chắc chắn có dữ liệu nên Compiler sẽ không báo vàng nữa
            var customer = await PartnerDataService.GetCustomerByEmailAsync(userData.Email);

            // Đề phòng trường hợp database lỗi không tìm thấy khách hàng
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 3. Lấy danh sách Tỉnh/Thành
            ViewBag.Provinces = await SelectListHelper.Provinces();

            // 4. Truyền luôn giỏ hàng sang View bằng ViewBag để hiển thị tóm tắt
            ViewBag.Cart = cart;

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> InitOrder(string deliveryProvince, string deliveryAddress)
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
            {
                // Báo lỗi bằng TempData để hiển thị ở trang trước nếu cần
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ địa chỉ giao hàng!";
                return RedirectToAction("Checkout");
            }

            // Gọi hàm AddOrderAsync để lưu đơn hàng và lấy về Mã đơn hàng (orderID)
            int customerId = int.Parse(userData.UserId);
            int orderId = await SalesDataService.AddOrderAsync(customerId, deliveryProvince, deliveryAddress, cart);

            if (orderId > 0)
            {
                // Nếu lưu thành công -> XÓA SẠCH GIỎ HÀNG
                ShoppingCartHelper.ClearCart();

                // Lưu ID đơn hàng vào TempData để hiển thị thông báo ở trang lịch sử (nếu muốn)
                TempData["SuccessMessage"] = $"Đặt hàng thành công!";

                // Chuyển hướng sang trang Lịch sử đơn hàng (Ta sẽ làm trang này tiếp theo)
                return RedirectToAction("History");
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại sau!";
                return RedirectToAction("Checkout");
            }
        }

        [HttpGet]
        public async Task<IActionResult> History(int page = 1)
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = int.Parse(userData.UserId);

            // Tạo input lấy dữ liệu (Tạm thời lấy PageSize to một chút để lọc)
            var input = new OrderSearchInput()
            {
                Page = page,
                PageSize = 200,
                SearchValue = ""
            };

            // Lấy danh sách từ CSDL
            var result = await SalesDataService.ListOrdersAsync(input);

            // 🔐 BẢO MẬT: Chỉ lọc ra đúng những đơn hàng của Khách hàng đang đăng nhập
            var myOrders = result.DataItems.Where(o => o.CustomerID == customerId)
                                           .OrderByDescending(o => o.OrderTime) // Sắp xếp đơn mới nhất lên đầu
                                           .ToList();

            return View(myOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id = 0)
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
                return RedirectToAction("Login", "Account");

            int customerId = int.Parse(userData.UserId);

            // 1. Lấy thông tin đơn hàng
            var order = await SalesDataService.GetOrderAsync(id);

            // 2. 🔐 BẢO MẬT: Chặn không cho người này xem đơn của người khác (Bằng cách gõ ID bậy bạ trên URL)
            if (order == null || order.CustomerID != customerId)
            {
                return RedirectToAction("History");
            }

            // 3. Lấy danh sách mặt hàng trong đơn
            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Details = details;

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id = 0)
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId))
                return RedirectToAction("Login", "Account");

            int customerId = int.Parse(userData.UserId);
            var order = await SalesDataService.GetOrderAsync(id);

            // Kiểm tra bảo mật
            if (order == null || order.CustomerID != customerId)
                return RedirectToAction("History");

            // Chỉ cho phép khách hàng tự hủy khi đơn "Vừa tạo" (New)
            if (order.Status != OrderStatusEnum.New)
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng khi shop chưa xét duyệt!";
                return RedirectToAction("Details", new { id = id });
            }

            // Gọi hàm hủy đơn (Truyền EmployeeID = 0 hoặc -1 vì đây là Khách hàng tự hủy, không phải nhân viên)
            bool result = await SalesDataService.CancelOrderAsync(id, 0);

            if (result)
                TempData["SuccessMessage"] = "Bạn đã hủy đơn hàng thành công!";
            else
                TempData["ErrorMessage"] = "Có lỗi xảy ra, không thể hủy đơn hàng lúc này.";

            return RedirectToAction("Details", new { id = id });
        }
    }
}