namespace OnlineShopingAppliaction.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        //Product
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string? ProductName {  get; set; }
       
        //Quentity
        public int Quantity { get; set; }

        //User
        public int UserId { get; set; }
        public AppUser User { get; set; }

        //  Added UserId to track who owns the cart item
       
    }
}
