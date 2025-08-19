using System.ComponentModel.DataAnnotations;

namespace OnlineShopingAppliaction.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty; 

        [Required]
        public decimal UnitPrice { get; set; } 

        [Required]
        public int Quantity { get; set; }

        //  values after discount at line level
        public decimal LineDiscount { get; set; }
        public decimal LineTotal { get; set; }


     
    }
}
