using SV22T1020782.DataLayers.Interfaces;
using SV22T1020782.DataLayers.SQLServer;
using SV22T1020782.Models;
using SV22T1020782.Models.Common;
using SV22T1020782.Models.HR;

namespace SV22T1020782.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        /// <param name="input">
        /// Thông tin tìm kiếm và phân trang (từ khóa tìm kiếm, trang cần hiển thị, số dòng mỗi trang).
        /// </param>
        /// <returns>
        /// Kết quả tìm kiếm dưới dạng danh sách nhân viên có phân trang.
        /// </returns>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần tìm.</param>
        /// <returns>
        /// Đối tượng Employee nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            if (employeeID <= 0) return null;

            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào hệ thống.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần bổ sung.</param>
        /// <returns>Mã nhân viên được tạo mới.</returns>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            //Kiểm tra dữ liệu hợp lệ
            // 1. Kiểm tra các trường thông tin bắt buộc
            if (string.IsNullOrWhiteSpace(data.FullName)) return 0;
            if (string.IsNullOrWhiteSpace(data.Email)) return 0;

            // 2. Kiểm tra tính hợp lệ của Email (Không được trùng lặp)
            bool isValidEmail = await employeeDB.ValidateEmailAsync(data.Email, 0); // 0 vì là nhân viên mới
            if (!isValidEmail) return 0;

            return await employeeDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            //Kiểm tra dữ liệu hợp lệ
            // 1. Kiểm tra các trường thông tin bắt buộc
            if (data.EmployeeID <= 0) return false;
            if (string.IsNullOrWhiteSpace(data.FullName)) return false;
            if (string.IsNullOrWhiteSpace(data.Email)) return false;

            // 2. Kiểm tra tính hợp lệ của Email (Không trùng với người KHÁC)
            bool isValidEmail = await employeeDB.ValidateEmailAsync(data.Email, data.EmployeeID);
            if (!isValidEmail) return false;

            return await employeeDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, False nếu nhân viên đang được sử dụng
        /// hoặc việc xóa không thực hiện được.
        /// </returns>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (employeeID <= 0) return false;

            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra.</param>
        /// <returns>
        /// True nếu nhân viên đang được sử dụng, ngược lại False.
        /// </returns>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            if (employeeID <= 0) return false;

            return await employeeDB.IsUsedAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// (không bị trùng với email của nhân viên khác).
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="employeeID">
        /// Nếu employeeID = 0: kiểm tra email đối với nhân viên mới.
        /// Nếu employeeID khác 0: kiểm tra email của nhân viên có mã là employeeID.
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (không trùng), ngược lại False.
        /// </returns>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        public static async Task<bool> UpdateEmployeeRolesAsync(string email, string roles)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await employeeDB.UpdateRolesAsync(email, roles);
        }

        public static async Task<bool> ChangeEmployeePasswordAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return false;

            password = CryptHelper.HashMD5(password);
            return await employeeDB.ChangePasswordAsync(email, password);
        }

        public static async Task<bool> VerifyEmployeePasswordAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return false;

            password = CryptHelper.HashMD5(password);
            return await employeeDB.VerifyPasswordAsync(email, password);
        }
        #endregion
    }
}