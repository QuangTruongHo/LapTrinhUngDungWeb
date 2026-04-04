namespace SV22T1020488.Shop.Models
{
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Photo { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Quantity * SalePrice;
    }
}