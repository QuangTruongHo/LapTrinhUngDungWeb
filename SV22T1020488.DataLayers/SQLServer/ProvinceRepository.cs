using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.DataDictionary;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho tỉnh thành từ SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách tất cả các tỉnh thành từ bảng Provinces
        /// </summary>
        /// <returns></returns>
        public async Task<List<Province>> ListAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

                // Sử dụng Dapper để map dữ liệu trực tiếp vào List<Province>
                var list = await connection.QueryAsync<Province>(sql);
                return list.ToList();
            }
        }
    }
}