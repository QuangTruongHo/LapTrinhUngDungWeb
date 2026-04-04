using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp trên SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới. Trả về ID của nhà cung cấp vừa tạo.
        /// </summary>
        public async Task<int> AddAsync(Supplier data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                            VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        /// <summary>
        /// Xóa một nhà cung cấp dựa trên mã ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhà cung cấp
        /// </summary>
        public async Task<Supplier?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
                var result = await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
                return result;
            }
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được sử dụng ở bảng khác (ví dụ: bảng Products) hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Products WHERE SupplierID = @SupplierID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { SupplierID = id });
                return result == 1;
            }
        }

        /// <summary>
        /// Truy vấn và phân trang danh sách nhà cung cấp dựa trên từ khóa tìm kiếm
        /// </summary>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Xử lý giá trị tìm kiếm
                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                // Câu lệnh lấy dữ liệu phân trang và tổng số dòng đồng thời
                var sql = @"
                    SELECT COUNT(*) FROM Suppliers 
                    WHERE (@SearchValue = '') OR (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);

                    SELECT * FROM Suppliers 
                    WHERE (@SearchValue = '') OR (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    SearchValue = searchValue,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Supplier>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp hiện có
        /// </summary>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Suppliers 
                            SET SupplierName = @SupplierName, 
                                ContactName = @ContactName, 
                                Province = @Province, 
                                Address = @Address, 
                                Phone = @Phone, 
                                Email = @Email
                            WHERE SupplierID = @SupplierID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}