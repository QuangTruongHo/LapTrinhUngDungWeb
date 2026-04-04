using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho loại hàng (Category) trên SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một loại hàng mới. Trả về ID của loại hàng vừa tạo.
        /// </summary>
        public async Task<int> AddAsync(Category data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Categories(CategoryName, Description)
                            VALUES(@CategoryName, @Description);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        /// <summary>
        /// Xóa một loại hàng dựa trên mã ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Categories WHERE CategoryID = @CategoryID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một loại hàng
        /// </summary>
        public async Task<Category?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Categories WHERE CategoryID = @CategoryID";
                return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem loại hàng này có đang được sử dụng bởi mặt hàng (Products) nào không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Products WHERE CategoryID = @CategoryID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { CategoryID = id });
                return result == 1;
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách loại hàng
        /// </summary>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                var sql = @"
                    SELECT COUNT(*) FROM Categories 
                    WHERE (@SearchValue = '') OR (CategoryName LIKE @SearchValue);

                    SELECT * FROM Categories 
                    WHERE (@SearchValue = '') OR (CategoryName LIKE @SearchValue)
                    ORDER BY CategoryName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    SearchValue = searchValue,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Category>()).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của một loại hàng hiện có
        /// </summary>
        public async Task<bool> UpdateAsync(Category data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Categories 
                            SET CategoryName = @CategoryName, 
                                Description = @Description
                            WHERE CategoryID = @CategoryID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}