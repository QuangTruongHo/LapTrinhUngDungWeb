using System.Security.Claims;
using System.Collections.Generic;

namespace SV22T1020488.Shop
{
    public class WebUserData
    {
        public string UserID { get; set; } = "";
        public string UserName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Photo { get; set; } = "";
        public string Phone { get; set; } = "";
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Lấy thông tin người dùng từ ClaimsPrincipal (User)
        /// </summary>
        public static WebUserData? GetUserData(ClaimsPrincipal user)
        {
            // Kiểm tra nếu chưa đăng nhập hoặc identity rỗng thì trả về null
            if (user.Identity == null || !user.Identity.IsAuthenticated)
                return null;

            // Khởi tạo đối tượng và nạp dữ liệu từ các Claims
            var userData = new WebUserData
            {
                // Lấy UserID (thường là ID số hoặc chuỗi định danh duy nhất)
                UserID = user.FindFirstValue("UserID") ?? "",

                // Lấy tên đăng nhập
                UserName = user.Identity.Name ?? "",

                // Lấy tên hiển thị (Ưu tiên claim DisplayName, không có thì lấy UserName)
                DisplayName = user.FindFirstValue("DisplayName") ?? user.Identity.Name ?? "",

                // Lấy Email
                Email = user.FindFirstValue(ClaimTypes.Email) ?? "",

                // Lấy Số điện thoại (Đảm bảo lúc Login bạn đã dùng ClaimTypes.MobilePhone)
                Phone = user.FindFirstValue(ClaimTypes.MobilePhone) ?? user.FindFirstValue("Phone") ?? "",

                // Lấy ảnh đại diện (nếu có)
                Photo = user.FindFirstValue("Photo") ?? ""
            };

            // Lấy danh sách quyền (Roles) nếu cần
            var roleClaims = user.FindAll(ClaimTypes.Role);
            userData.Roles = new List<string>();
            foreach (var rc in roleClaims)
            {
                userData.Roles.Add(rc.Value);
            }

            return userData;
        }
    }
}