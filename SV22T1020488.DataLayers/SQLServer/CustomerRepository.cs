using Dapper;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customer) trên SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một khách hàng mới. Trả về ID của khách hàng vừa tạo.
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                            VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        /// <summary>
        /// Xóa khách hàng dựa trên mã ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Customers WHERE CustomerID = @CustomerID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { CustomerID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một khách hàng
        /// </summary>
        public async Task<Customer?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Customers WHERE CustomerID = @CustomerID";
                return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có đang phát sinh dữ liệu liên quan (ví dụ: có đơn hàng) hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Orders WHERE CustomerID = @CustomerID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { CustomerID = id });
                return result == 1;
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách khách hàng
        /// </summary>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                var sql = @"
                    SELECT COUNT(*) FROM Customers 
                    WHERE (@SearchValue = '') 
                       OR (CustomerName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                       OR (Phone LIKE @SearchValue);

                    SELECT * FROM Customers 
                    WHERE (@SearchValue = '') 
                       OR (CustomerName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                       OR (Phone LIKE @SearchValue)
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    SearchValue = searchValue,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Customer>()).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Customers 
                            SET CustomerName = @CustomerName, 
                                ContactName = @ContactName, 
                                Province = @Province, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email,
                                IsLocked = @IsLocked
                            WHERE CustomerID = @CustomerID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem địa chỉ Email đã được sử dụng bởi khách hàng khác hay chưa.
        /// Trả về true nếu email hợp lệ (chưa bị trùng), ngược lại trả về false.
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Nếu ID = 0: Kiểm tra xem email đã tồn tại trong bảng chưa
                // Nếu ID <> 0: Kiểm tra xem email đã tồn tại ở bản ghi khác (trừ bản ghi hiện tại) chưa
                var sql = @"IF EXISTS (SELECT 1 FROM Customers WHERE Email = @Email AND CustomerID <> @CustomerID)
                                SELECT 0
                            ELSE
                                SELECT 1";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, CustomerID = id });
                return result == 1;
            }
        }
    }
}