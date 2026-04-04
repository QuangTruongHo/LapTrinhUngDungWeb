public static class ApplicationContext
{
    public const string SESSION_CART = "CartData";

    // Bạn có thể thêm đường dẫn ảnh sản phẩm mặc định ở đây
    public static string ProductImage(string? photo)
    {
        if (string.IsNullOrEmpty(photo)) return "/images/products/no-image.png";
        return $"/images/products/{photo}";
    }
}