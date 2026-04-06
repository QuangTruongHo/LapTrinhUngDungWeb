using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace SV22T1020488.Shop
{
    public static class ApplicationContext
    {
        public const string SESSION_CART = "CartData";
        private static IHttpContextAccessor? _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static HttpContext Current => _httpContextAccessor?.HttpContext
                                            ?? throw new Exception("HttpContextAccessor not configured.");

        /// <summary>
        /// Trả về đường dẫn ảnh sản phẩm, nếu không có ảnh dùng ảnh mặc định
        /// </summary>
        public static string ProductImage(string? photo)
        {
            if (string.IsNullOrEmpty(photo)) return "/images/products/no-image.png";
            return $"/images/products/{photo}";
        }

        /// <summary>
        /// Lưu dữ liệu vào Session
        /// </summary>
        public static void SetSessionData<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            Current.Session.SetString(key, json);
        }

        /// <summary>
        /// Lấy dữ durable từ Session
        /// </summary>
        public static T? GetSessionData<T>(string key)
        {
            var json = Current.Session.GetString(key);
            return json == null ? default(T) : JsonConvert.DeserializeObject<T>(json);
        }
    }
}