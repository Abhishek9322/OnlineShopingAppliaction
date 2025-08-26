namespace OnlineShopingAppliaction.Models
{
    public class CheckoutViewModel
    {
        // If cartItemId is set, we're checking out a single cart item, otherwise whole cart
        public int? CartItemId { get; set; }

        // Shipping fields
        public string ShippingFullName { get; set; }
        public string ShippingPhone { get; set; }
        public string ShippingAddressLine1 { get; set; }
        public string ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingCountry { get; set; } = "India";
        public string ShippingPincode { get; set; }
        public string ShippingNotes { get; set; }

        // Items & totals (populated by GET)
        public List<CartItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalTotal { get; set; }
    }

}
