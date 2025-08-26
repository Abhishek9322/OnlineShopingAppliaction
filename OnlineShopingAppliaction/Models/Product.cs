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

        [Range(0, int.MaxValue, ErrorMessage = "Stock must be >=0")]
        public int Stock { get; set; }

        [Required]
        public int OwnerId { get; set; }
        public AppUser? Owner { get; set; }

      //  public bool IsDeleted { get; set; } = false;

    }
}
