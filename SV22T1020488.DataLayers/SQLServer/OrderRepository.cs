using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.Models.Common;
using SV22T1020488.Models.Sales;
using System.Data;

namespace SV22T1020488.DataLayers.SQLServer
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Quản lý Đơn hàng (Orders)

        public async Task<int> AddAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, 
                                               EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                            VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, 
                                   @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Orders 
                            SET CustomerID = @CustomerID,
                                OrderTime = @OrderTime,
                                DeliveryProvince = @DeliveryProvince,
                                DeliveryAddress = @DeliveryAddress,
                                EmployeeID = @EmployeeID,
                                AcceptTime = @AcceptTime,
                                ShipperID = @ShipperID,
                                ShippedTime = @ShippedTime,
                                FinishedTime = @FinishedTime,
                                Status = @Status
                            WHERE OrderID = @OrderID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Xóa chi tiết đơn hàng trước khi xóa đơn hàng chính
                var sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                            DELETE FROM Orders WHERE OrderID = @OrderID;";
                return await connection.ExecuteAsync(sql, new { OrderID = orderID }) > 0;
            }
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT o.*, 
                                   c.CustomerName, c.ContactName as CustomerContact, c.Phone as CustomerPhone, c.Email as CustomerEmail,
                                   e.FullName as EmployeeName,
                                   s.ShipperName, s.Phone as ShipperPhone
                            FROM Orders as o
                            LEFT JOIN Customers as c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees as e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers as s ON o.ShipperID = s.ShipperID
                            WHERE o.OrderID = @OrderID";
                return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
            }
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>() { Page = input.Page, PageSize = input.PageSize };
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string searchValue = string.IsNullOrEmpty(input.SearchValue) ? "" : $"%{input.SearchValue}%";

                var sql = @"
                    SELECT COUNT(*) 
                    FROM Orders as o
                    LEFT JOIN Customers as c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees as e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers as s ON o.ShipperID = s.ShipperID
                    WHERE (@Status = 0 OR o.Status = @Status)
                      AND (@FromTime IS NULL OR o.OrderTime >= @FromTime)
                      AND (@ToTime IS NULL OR o.OrderTime <= @ToTime)
                      AND (@SearchValue = '' OR c.CustomerName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue);

                    SELECT o.*, 
                           c.CustomerName, e.FullName as EmployeeName, s.ShipperName
                    FROM Orders as o
                    LEFT JOIN Customers as c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees as e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers as s ON o.ShipperID = s.ShipperID
                    WHERE (@Status = 0 OR o.Status = @Status)
                      AND (@FromTime IS NULL OR o.OrderTime >= @FromTime)
                      AND (@ToTime IS NULL OR o.OrderTime <= @ToTime)
                      AND (@SearchValue = '' OR c.CustomerName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue)
                    ORDER BY o.OrderID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    Status = input.Status,
                    FromTime = input.DateFrom,
                    ToTime = input.DateTo,
                    SearchValue = searchValue,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                }))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<OrderViewInfo>()).ToList();
                }
            }
            return result;
        }

        #endregion

        #region Quản lý Chi tiết đơn hàng (OrderDetails)

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT od.*, p.ProductName, p.Photo, p.Unit
                            FROM OrderDetails as od
                            JOIN Products as p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID";
                return (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
            }
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT od.*, p.ProductName, p.Photo, p.Unit
                            FROM OrderDetails as od
                            JOIN Products as p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID";
                return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
            }
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID)
                                UPDATE OrderDetails 
                                SET Quantity = Quantity + @Quantity, SalePrice = @SalePrice
                                WHERE OrderID = @OrderID AND ProductID = @ProductID
                            ELSE
                                INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                                VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE OrderDetails 
                            SET Quantity = @Quantity, SalePrice = @SalePrice
                            WHERE OrderID = @OrderID AND ProductID = @ProductID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
                return await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID }) > 0;
            }
        }

        #endregion

        public async Task<int> CountOrdersAsync(int status = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT COUNT(*) FROM Orders WHERE (@Status = 0 OR Status = @Status)";
                return await connection.ExecuteScalarAsync<int>(sql, new { Status = status });
            }
        }

        public async Task<decimal> GetRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Doanh thu chỉ tính trên các đơn hàng đã hoàn thành (giả sử Status = 4 là thành công)
                var sql = @"SELECT SUM(od.Quantity * od.SalePrice) 
                    FROM OrderDetails od 
                    JOIN Orders o ON od.OrderID = o.OrderID 
                    WHERE o.Status = 4 AND o.OrderTime BETWEEN @fromDate AND @toDate";
                return await connection.ExecuteScalarAsync<decimal>(sql, new { fromDate, toDate });
            }
        }
    }
}