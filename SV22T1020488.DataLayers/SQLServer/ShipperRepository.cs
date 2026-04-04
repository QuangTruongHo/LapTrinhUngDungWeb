using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Partner;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng (Shipper) trên SQL Server
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một người giao hàng mới. Trả về ID vừa tạo.
        /// </summary>
        public async Task<int> AddAsync(Shipper data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Shippers(ShipperName, Phone)
                            VALUES(@ShipperName, @Phone);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        /// <summary>
        /// Xóa một người giao hàng theo mã ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Shippers WHERE ShipperID = @ShipperID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ShipperID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một người giao hàng
        /// </summary>
        public async Task<Shipper?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
                return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem người giao hàng này đã có đơn hàng nào chưa?
        /// (Dựa trên bảng Orders)
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Orders WHERE ShipperID = @ShipperID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                var result = await connection.ExecuteScalarAsync<int>(sql, new { ShipperID = id });
                return result == 1;
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách người giao hàng
        /// </summary>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                var sql = @"
                    SELECT COUNT(*) FROM Shippers 
                    WHERE (@SearchValue = '') OR (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);

                    SELECT * FROM Shippers 
                    WHERE (@SearchValue = '') OR (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    SearchValue = searchValue,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Shipper>()).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Shippers 
                            SET ShipperName = @ShipperName, 
                                Phone = @Phone
                            WHERE ShipperID = @ShipperID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}