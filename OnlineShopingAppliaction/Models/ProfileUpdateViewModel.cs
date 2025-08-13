using System.ComponentModel.DataAnnotations;

namespace OnlineShopingAppliaction.Models
{
    public class ProfileUpdateViewModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Username")]
        public string UserName { get; set; }

        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; }

        //[Display(Name = "Full Name")]
        //public string FullName { get; set; }

        [DataType(DataType.Password), Display(Name = "New Password")]
        public string? NewPassword { get; set; }
    }
}
