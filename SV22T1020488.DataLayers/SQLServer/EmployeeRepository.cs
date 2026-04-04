using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.HR;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên (Employee) trên SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một nhân viên mới. Trả về ID của nhân viên vừa tạo.
        /// </summary>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                            VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        /// <summary>
        /// Xóa nhân viên dựa trên mã ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { EmployeeID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên
        /// </summary>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
                return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có dữ liệu liên quan (ví dụ: đã từng lập đơn hàng) hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeID = id });
                return result == 1;
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách nhân viên
        /// </summary>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                var sql = @"
                    SELECT COUNT(*) FROM Employees 
                    WHERE (@SearchValue = '') 
                       OR (FullName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue);

                    SELECT * FROM Employees 
                    WHERE (@SearchValue = '') 
                       OR (FullName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY FullName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    SearchValue = searchValue,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Employee>()).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Employees 
                            SET FullName = @FullName, 
                                BirthDate = @BirthDate, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email,
                                Photo = @Photo,
                                IsWorking = @IsWorking,
                                RoleNames = @RoleNames
                            WHERE EmployeeID = @EmployeeID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra email nhân viên có trùng với người khác không
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Employees WHERE Email = @Email AND EmployeeID <> @EmployeeID)
                                SELECT 0
                            ELSE
                                SELECT 1";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
                return result == 1;
            }
        }

        /// <summary>
        /// Đổi mật khẩu cho nhân viên
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int employeeID, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Chú ý: Đảm bảo tên cột trong DB là 'Password' 
                // Nếu DB của bạn dùng tên khác (ví dụ: PasswordHash), hãy sửa lại ở đây.
                var sql = @"UPDATE Employees 
                            SET [Password] = @Password 
                            WHERE EmployeeID = @EmployeeID";

                // Thực thi câu lệnh SQL với tham số tường minh
                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    EmployeeID = employeeID,
                    Password = newPassword // Đây là chuỗi đã được mã hóa MD5 từ Service truyền xuống
                });

                return rowsAffected > 0;
            }
        }
    }
}