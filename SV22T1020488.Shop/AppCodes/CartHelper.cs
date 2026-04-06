using Newtonsoft.Json;
using SV22T1020488.Shop.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020488.Shop
{
    public static class CartHelper
    {
        // Tên cookie cho khách vãng lai
        private const string GUEST_CART = "Cart_Guest";

        private static string GetCartCookieName(HttpContext context)
        {
            var customerId = context.Session.GetString("CustomerID");
            if (!string.IsNullOrEmpty(customerId))
            {
                return $"Cart_User_{customerId}";
            }
            return GUEST_CART;
        }

        public static List<CartItem> GetCart(HttpContext context)
        {
            string cookieName = GetCartCookieName(context);
            var cookieData = context.Request.Cookies[cookieName];

            if (string.IsNullOrEmpty(cookieData)) return new List<CartItem>();

            try
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(cookieData) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        public static void SaveCart(HttpContext context, List<CartItem> cart)
        {
            string cookieName = GetCartCookieName(context);
            string jsonCart = JsonConvert.SerializeObject(cart);

            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(7),
                HttpOnly = true,
                IsEssential = true,
                Path = "/" // Quan trọng: Để cookie có hiệu lực toàn trang
            };

            context.Response.Cookies.Append(cookieName, jsonCart, options);
        }

        // HÀM MỚI: Gộp giỏ khách vào giỏ người dùng khi đăng nhập
        public static void MergeCart(HttpContext context)
        {
            // 1. Lấy giỏ hàng từ khách vãng lai
            var guestData = context.Request.Cookies[GUEST_CART];
            if (string.IsNullOrEmpty(guestData)) return;

            var guestCart = JsonConvert.DeserializeObject<List<CartItem>>(guestData);
            if (guestCart == null || guestCart.Count == 0) return;

            // 2. Lấy giỏ hàng hiện tại của User (nếu có)
            var userCart = GetCart(context); // Lúc này đã có CustomerID trong Session nên nó sẽ lấy Cart_User_ID

            // 3. Gộp hàng
            foreach (var item in guestCart)
            {
                var existingItem = userCart.FirstOrDefault(x => x.ProductID == item.ProductID);
                if (existingItem != null)
                    existingItem.Quantity += item.Quantity;
                else
                    userCart.Add(item);
            }

            // 4. Lưu giỏ hàng mới cho User và Xóa giỏ khách
            SaveCart(context, userCart);
            context.Response.Cookies.Delete(GUEST_CART);
        }

        public static void ClearCart(HttpContext context)
        {
            string cookieName = GetCartCookieName(context);
            context.Response.Cookies.Delete(cookieName);
        }
    }
}