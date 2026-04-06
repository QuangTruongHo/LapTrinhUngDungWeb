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


       

        // 2. Đăng nhập Shop (Bảng Customers)
        public async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT CAST(CustomerID AS NVARCHAR) AS UserId, Email AS UserName, 
                        CustomerName AS DisplayName, N'' AS Photo, N'customer' AS RoleNames
                        FROM Customers WHERE Email = @userName AND Password = @password 
                        AND (IsLocked = 0 OR IsLocked IS NULL)";
                return await cn.QueryFirstOrDefaultAsync<UserAccount>(sql, new { userName, password });
            }
        }

        // 3. Đổi mật khẩu Khách hàng
        public async Task<bool> ChangeCustomerPasswordAsync(string userName, string password)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE Customers SET Password = @password WHERE Email = @userName";
                return await cn.ExecuteAsync(sql, new { userName, password }) > 0;
            }
        }

        // 4. Đăng ký Khách hàng mới (Tách biệt hoàn toàn với Employees)
        public async Task<bool> RegisterCustomerAsync(string customerName, string email, string password)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                var sql = @"INSERT INTO Customers (CustomerName, ContactName, Email, Password, IsLocked)
                        VALUES (@customerName, @customerName, @email, @password, 0)";
                return await cn.ExecuteAsync(sql, new { customerName, email, password }) > 0;
            }
        }

        public async Task<bool> CheckCustomerEmailExistsAsync(string email)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                var sql = "SELECT COUNT(*) FROM Customers WHERE Email = @email";
                return await cn.ExecuteScalarAsync<int>(sql, new { email }) > 0;
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