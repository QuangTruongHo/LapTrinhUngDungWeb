using System.Security.Claims;

namespace SV22T1020488.Shop // Đảm bảo namespace này thống nhất
{
    public class WebUserData
    {
        public string UserID { get; set; } = "";
        public string UserName { get; set; } = "";
        public string DisplayName { get; set; } = ""; // Cần thêm thuộc tính này
        public string Email { get; set; } = "";
        public string Photo { get; set; } = "";
        public List<string>? Roles { get; set; }

        // Đổi tên hàm từ Get thành GetUserData để khớp với View bạn đang viết
        public static WebUserData? GetUserData(ClaimsPrincipal user)
        {
            if (!user.Identity!.IsAuthenticated) return null;
            return new WebUserData
            {
                UserID = user.FindFirstValue("UserID") ?? "",
                UserName = user.Identity.Name ?? "",
                // Giả sử DisplayName được lưu trong Claim "DisplayName" hoặc lấy tạm UserName
                DisplayName = user.FindFirstValue("DisplayName") ?? user.Identity.Name ?? "",
                Email = user.FindFirstValue(ClaimTypes.Email) ?? ""
            };
        }
    }
}