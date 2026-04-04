using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020782.BusinessLayers;

namespace SV22T1020782.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đủ thông tin");
                return View();
            }

            // 2. Gọi xuống BusinessLayer → DB thật
            var userAccount = await SecurityDataService.AuthorizeAsync(username, password);

            // 3. Nếu sai tài khoản
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Thông tin tài khoản không hợp lệ");
                return View();
            }

            // 4. Tạo dữ liệu lưu cookie
            var webUserData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,

                // ⚠️ DB của bạn dùng dấu ; (employee) hoặc , (bạn đang dùng)
                Roles = userAccount.RoleNames
                            .Replace(";", ",")
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .ToList()
            };

            // 5. Ghi cookie đăng nhập
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                webUserData.CreatePrincipal()
            );

            // 6. Redirect
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // 1. Validate
            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View();
            }

            // 2. Lấy user hiện tại từ cookie
            var user = User.GetUserData();
            if (user == null)
                return RedirectToAction("Login");

            // 3. Kiểm tra mật khẩu cũ
            bool isValid = await SecurityDataService.VerifyPasswordAsync(user.UserName!, oldPassword);
            if (!isValid)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng");
                return View();
            }

            // 4. Đổi mật khẩu
            bool result = await SecurityDataService.ChangePasswordAsync(user.UserName!, newPassword);
            if (!result)
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại");
                return View();
            }

            // 5. Thành công
            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Logout()
        {
            User.GetUserData();

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            ViewBag.Title = "Tài khoản không đuoc phép truy cập chuc nang này";
            return View();
        }
    }
}
