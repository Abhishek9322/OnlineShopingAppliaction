using System.ComponentModel.DataAnnotations;

namespace OnlineShopingAppliaction.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [RegularExpression(@"^(?=.*[A-Za-z])[A-Za-z0-9]{1,}$",
            ErrorMessage = "Username must contain at least one letter and cannot be all numbers.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string PasswordHash { get; set; }  // Use BCrypt

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } // "Admin" or "User"

        public ICollection<CartItem> CartItems { get; set; }   // Navigation property 

    }
}
