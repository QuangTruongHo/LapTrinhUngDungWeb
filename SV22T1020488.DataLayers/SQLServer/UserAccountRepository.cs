using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Security;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản người dùng trên SQL Server
    /// </summary>
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public UserAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Xác thực người dùng dựa trên Email và Password.
        /// </summary>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Câu lệnh SQL lấy thông tin nhân viên
                var sql = @"SELECT CAST(EmployeeID AS NVARCHAR) AS UserId,
                                   Email AS UserName,
                                   FullName AS DisplayName,
                                   Email,
                                   Photo,
                                   RoleNames
                            FROM Employees
                            WHERE Email = @userName AND Password = @password AND IsWorking = 1";

                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
                {
                    userName = userName,
                    password = password
                });
            }
        }

        /// <summary>
        /// Đổi mật khẩu cho nhân viên
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Employees 
                            SET Password = @password 
                            WHERE Email = @userName";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    userName = userName,
                    password = password
                });

                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Đăng ký tài khoản mới (Thêm mới một nhân viên/người dùng)
        /// </summary>
        public async Task<bool> RegisterAsync(UserAccount data, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Employees (FullName, Email, Password, Photo, RoleNames, IsWorking)
                            VALUES (@DisplayName, @Email, @Password, @Photo, @RoleNames, 1)";

                var parameters = new
                {
                    DisplayName = data.DisplayName,
                    Email = data.Email,
                    Password = password, // Mật khẩu đã được băm MD5 từ Controller
                    Photo = string.IsNullOrEmpty(data.Photo) ? "user.png" : data.Photo,
                    RoleNames = "customer" // Mặc định gán quyền khách hàng cho người đăng ký mới
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem Email đã tồn tại trong hệ thống chưa
        /// </summary>
        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT COUNT(*) FROM Employees WHERE Email = @email";

                var count = await connection.ExecuteScalarAsync<int>(sql, new { email = email });
                return count > 0;
            }
        }
    }
}