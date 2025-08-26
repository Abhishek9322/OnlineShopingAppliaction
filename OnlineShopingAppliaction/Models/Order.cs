using System.ComponentModel.DataAnnotations;

namespace OnlineShopingAppliaction.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public AppUser User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }

        public string Status { get; set; } = "Placed";

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();


        // NEW FIELD: Assign Delivery Boy
        public int? DeliveryBoyId { get; set; }
        public AppUser DeliveryBoy { get; set; }


        [Required(ErrorMessage = "Full name is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full name can only contain letters and spaces")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 50 characters")]
        public string ShippingFullName { get; set; }

       
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits")]
        public string ShippingPhone { get; set; }


        [Required(ErrorMessage = "Address line 1 is required")]
        [RegularExpression(@"^[A-Za-z\s,.-/]+$", ErrorMessage = "Address line 1 can only contain letters, spaces, comma, dash, and slash")]
        public string ShippingAddressLine1 { get; set; }

        [RegularExpression(@"^[A-Za-z\s,.-/]*$", ErrorMessage = "Address line 2 can only contain letters, spaces, comma, dash, and slash")]
        public string ShippingAddressLine2 { get; set; }


        [Required(ErrorMessage = "City is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "City can only contain letters and spaces")]
        public string ShippingCity { get; set; }

        
        [Required(ErrorMessage = "State is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "State can only contain letters and spaces")]
        public string ShippingState { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Country can only contain letters and spaces")]
        public string ShippingCountry { get; set; }


        [Required(ErrorMessage = "Shipping Pincode is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be exactly 6 digits")]
        public string ShippingPincode { get; set; }

        public string ShippingNotes { get; set; }

 


    }

}
