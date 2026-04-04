using SV22T1020488.DataLayers.Interfaces;
using SV22T1020488.DataLayers.SQLServer;
using SV22T1020488.Models.Security;

namespace SV22T1020488.BusinessLayers
{
    /// <summary>
    /// Các dịch vụ xử lý liên quan đến tài khoản người dùng
    /// </summary>
    public static class UserAccountService
    {
        private static readonly IUserAccountRepository userAccountDB;

        static UserAccountService()
        {
            // Chuỗi kết nối đến cơ sở dữ liệu
            string connectionString = @"Server=TRUONG; Database=LiteCommerceDB; User Id=sa; Password=12345678; TrustServerCertificate=True;";
            userAccountDB = new UserAccountRepository(connectionString);
        }

        /// <summary>
        /// Xác thực tài khoản (Đăng nhập)
        /// </summary>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            return await userAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            return await userAccountDB.ChangePasswordAsync(userName, password);
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="data">Thông tin tài khoản</param>
        /// <param name="password">Mật khẩu (đã băm)</param>
        /// <returns></returns>
        public static async Task<bool> RegisterAsync(UserAccount data, string password)
        {
            return await userAccountDB.RegisterAsync(data, password);
        }

        /// <summary>
        /// Kiểm tra Email đã tồn tại hay chưa
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await userAccountDB.CheckEmailExistsAsync(email);
        }
    }
}