using SV22T1020782.Models.HR;
using SV22T1020782.Models.Security;

namespace SV22T1020782.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int employeeID);
        Task<bool> UpdateRolesAsync(string email, string roles);
        Task<bool> ChangePasswordAsync(string email, string password);
        Task<bool> VerifyPasswordAsync(string email, string password);
    }
}
