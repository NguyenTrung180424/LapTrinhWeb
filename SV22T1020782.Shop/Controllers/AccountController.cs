using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;
using SV22T1020782.Models;
using SV22T1020782.Models.Partner;

namespace SV22T1020782.Shop.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì đá về trang chủ
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewBag.Email = email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ Email và Mật khẩu");
                return View();
            }

            // 1. Kiểm tra tài khoản và mật khẩu
            // 🔐 BẢO MẬT: Băm mật khẩu người dùng nhập vào để so sánh với mã MD5 trong DB
            string hashedPassword = CryptHelper.HashMD5(password);
            bool isValid = await PartnerDataService.VerifyCustomerPasswordAsync(email, hashedPassword);

            if (!isValid)
            {
                ModelState.AddModelError("Error", "Email hoặc mật khẩu không chính xác!");
                return View();
            }

            // 2. Lấy thông tin khách hàng
            var customer = await PartnerDataService.GetCustomerByEmailAsync(email);
            if (customer == null)
            {
                ModelState.AddModelError("Error", "Tài khoản không tồn tại!");
                return View();
            }

            // 3. Kiểm tra xem tài khoản có bị khóa không
            if (customer.IsLocked)
            {
                ModelState.AddModelError("Error", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên!");
                return View();
            }

            // 4. Tạo thông tin lưu vào Cookie (Sử dụng WebUserData đã copy từ Admin)
            var userData = new WebUserData()
            {
                UserId = customer.CustomerID.ToString(),
                UserName = customer.Email,
                DisplayName = customer.CustomerName,
                Email = customer.Email,
                Photo = "nophoto.png",
                Roles = new List<string>() { "Customer" }
            };

            // 5. Ghi nhận đăng nhập
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userData.CreatePrincipal());

            // 6. Quay về trang chủ
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Register() // Đổi thành async Task
        {
            // SỬA Ở ĐÂY: Dùng SelectListHelper
            ViewBag.Provinces = await SelectListHelper.Provinces();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Customer data, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập họ tên");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập Email");
            else if (!await PartnerDataService.ValidateCustomerEmailAsync(data.Email, 0))
                ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Vui lòng nhập mật khẩu");
            else if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            // BẮT BỘC CHỌN TỈNH THÀNH (Tránh lỗi Foreign Key)
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành phố");

            if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
            if (string.IsNullOrWhiteSpace(data.Address)) data.Address = "";
            if (string.IsNullOrWhiteSpace(data.Phone)) data.Phone = "";

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View(data);
            }

            // Thêm khách hàng mới
            int id = await PartnerDataService.AddCustomerAsync(data);
            if (id > 0)
            {
                // 🔐 BẢO MẬT: Băm mật khẩu ra mã MD5 trước khi lưu xuống DB
                string hashedPassword = CryptHelper.HashMD5(password);
                await PartnerDataService.ChangeCustomerPasswordAsync(data.Email, hashedPassword);

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("Error", "Đăng ký thất bại, vui lòng thử lại sau!");

            ViewBag.Provinces = await SelectListHelper.Provinces();
            return View(data);
        }

        [Authorize] // Bắt buộc phải đăng nhập mới được vào
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();
            // THÊM KIỂM TRA NULL CHO Email
            if (userData == null || string.IsNullOrEmpty(userData.Email))
                return RedirectToAction("Login");

            // Lấy dữ liệu khách hàng từ DB (Lúc này userData.Email chắc chắn không null)
            var customer = await PartnerDataService.GetCustomerByEmailAsync(userData.Email);
            if (customer == null) return RedirectToAction("Login");

            ViewBag.Provinces = await SelectListHelper.Provinces();
            return View(customer);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Profile(Customer data)
        {
            var userData = User.GetUserData();
            // THÊM KIỂM TRA NULL CHO UserId và Email
            if (userData == null || string.IsNullOrEmpty(userData.UserId) || string.IsNullOrEmpty(userData.Email))
                return RedirectToAction("Login");

            // Gán cứng ID và Email từ session đang đăng nhập để tránh user F12 đổi mã HTML
            data.CustomerID = int.Parse(userData.UserId);
            data.Email = userData.Email;

            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập họ tên");
            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành phố");

            if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
            if (string.IsNullOrWhiteSpace(data.Address)) data.Address = "";
            if (string.IsNullOrWhiteSpace(data.Phone)) data.Phone = "";

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View(data);
            }

            // Gọi hàm cập nhật
            bool result = await PartnerDataService.UpdateCustomerAsync(data);
            if (result)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("Error", "Cập nhật thất bại, vui lòng thử lại sau!");
            ViewBag.Provinces = await SelectListHelper.Provinces();
            return View(data);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userData = User.GetUserData();
            // THÊM KIỂM TRA NULL CHO Email
            if (userData == null || string.IsNullOrEmpty(userData.Email))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
                return View();
            }

            // 🔐 BẢO MẬT 1: Băm mật khẩu cũ để kiểm tra
            string hashedOldPassword = CryptHelper.HashMD5(oldPassword.Trim());
            bool isCorrect = await PartnerDataService.VerifyCustomerPasswordAsync(userData.Email, hashedOldPassword);
            if (!isCorrect)
            {
                ModelState.AddModelError("oldPassword", "Mật khẩu hiện tại không đúng");
                return View();
            }

            // 🔐 BẢO MẬT 2: Băm mật khẩu mới trước khi lưu
            string hashedNewPassword = CryptHelper.HashMD5(newPassword.Trim());
            bool result = await PartnerDataService.ChangeCustomerPasswordAsync(userData.Email, hashedNewPassword);

            if (result)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("ChangePassword");
            }

            ModelState.AddModelError("", "Đổi mật khẩu thất bại, vui lòng thử lại sau!");
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}