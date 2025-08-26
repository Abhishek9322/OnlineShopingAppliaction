using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface ICartRepository
    {
        Task<CartItem?> GetCartItemByIdAsync(int id);
        Task<CartItem?> GetUserCartItemAsync(int userId, int productId);
        Task<List<CartItem>> GetUserCartAsync(int userId);
        Task AddCartItemAsync(CartItem cartItem);
        Task RemoveCartItemAsync(CartItem cartItem);
        Task SaveAsync();
    }
}
