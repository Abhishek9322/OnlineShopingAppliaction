using System.ComponentModel.DataAnnotations;

namespace OnlineShopingAppliaction.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        public string Name { get; set; } = string.Empty;

        // Navigation property (one category has many products)
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
