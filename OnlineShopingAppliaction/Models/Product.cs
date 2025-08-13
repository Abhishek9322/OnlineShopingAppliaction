using System.ComponentModel.DataAnnotations;

namespace OnlineShopingAppliaction.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        public string Description { get; set; }
        [Required] public decimal Price { get; set; }
        public decimal Discount { get; set; } // discount in %

        public string? ImagePath { get; set; }
  

        //  Foreign Key
        [Required(ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        //Navigation Property
        public Category Category { get; set; }

    }
}
