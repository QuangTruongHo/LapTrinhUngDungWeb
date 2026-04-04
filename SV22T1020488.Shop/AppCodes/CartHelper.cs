using Newtonsoft.Json;
using SV22T1020488.Shop.Models;

namespace SV22T1020488.Shop
{
    public static class CartHelper
    {
        private const string CART_KEY = "MyCart";

        public static List<CartItem> GetCart(HttpContext context)
        {
            var sessionData = context.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(sessionData)) return new List<CartItem>();
            return JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>();
        }

        public static void SaveCart(HttpContext context, List<CartItem> cart)
        {
            context.Session.SetString(CART_KEY, JsonConvert.SerializeObject(cart));
        }

        public static void ClearCart(HttpContext context)
        {
            context.Session.Remove(CART_KEY);
        }
    }
}