using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Catalog;
using SV22T1020488.Models.Common;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Quản lý mặt hàng (Products)

        public async Task<int> AddAsync(Product data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                            VALUES(@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Products 
                            SET ProductName = @ProductName, 
                                ProductDescription = @ProductDescription, 
                                SupplierID = @SupplierID, 
                                CategoryID = @CategoryID, 
                                Unit = @Unit, 
                                Price = @Price, 
                                Photo = @Photo, 
                                IsSelling = @IsSelling
                            WHERE ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                            DELETE FROM ProductPhotos WHERE ProductID = @ProductID;
                            DELETE FROM Products WHERE ProductID = @ProductID;";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ProductID = productID });
                return rowsAffected > 0;
            }
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
                return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
            }
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<int>(sql, new { ProductID = productID }) == 1;
            }
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>() { Page = input.Page, PageSize = input.PageSize };
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                var sql = @"
                    SELECT COUNT(*) FROM Products 
                    WHERE (@SearchValue = '' OR ProductName LIKE @SearchValue)
                      AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                      AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                      AND (Price >= @MinPrice)
                      AND (@MaxPrice = 0 OR Price <= @MaxPrice);

                    SELECT * FROM Products 
                    WHERE (@SearchValue = '' OR ProductName LIKE @SearchValue)
                      AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                      AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                      AND (Price >= @MinPrice)
                      AND (@MaxPrice = 0 OR Price <= @MaxPrice)
                    ORDER BY ProductName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    SearchValue = searchValue,
                    CategoryID = input.CategoryID,
                    SupplierID = input.SupplierID,
                    MinPrice = input.MinPrice,
                    MaxPrice = input.MaxPrice,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Product>()).ToList();
                }
            }
            return result;
        }

        #endregion

        #region Quản lý ảnh (ProductPhotos)

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                return (await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID })).ToList();
            }
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
                return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
            }
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                            VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<long>(sql, data);
            }
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE ProductPhotos 
                            SET Photo = @Photo, Description = @Description, DisplayOrder = @DisplayOrder, IsHidden = @IsHidden
                            WHERE PhotoID = @PhotoID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
                return await connection.ExecuteAsync(sql, new { PhotoID = photoID }) > 0;
            }
        }

        #endregion

        #region Quản lý thuộc tính (ProductAttributes)

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                return (await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID })).ToList();
            }
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
                return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
            }
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                            VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<long>(sql, data);
            }
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE ProductAttributes 
                            SET AttributeName = @AttributeName, AttributeValue = @AttributeValue, DisplayOrder = @DisplayOrder
                            WHERE AttributeID = @AttributeID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
                return await connection.ExecuteAsync(sql, new { AttributeID = attributeID }) > 0;
            }
        }

        #endregion
    }
}