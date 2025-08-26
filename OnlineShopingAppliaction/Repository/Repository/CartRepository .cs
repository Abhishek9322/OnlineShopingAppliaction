using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Repository.Repository
{
    public class CartRepository: ICartRepository
    {
        private readonly ApplicationDbContext _context;
        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartItem?> GetCartItemByIdAsync(int id) =>
            await _context.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == id);

        public async Task<CartItem?> GetUserCartItemAsync(int userId, int productId) =>
            await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

        public async Task<List<CartItem>> GetUserCartAsync(int userId) =>
            await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();

        public async Task AddCartItemAsync(CartItem cartItem) =>
            await _context.CartItems.AddAsync(cartItem);

        public async Task RemoveCartItemAsync(CartItem cartItem) =>
            _context.CartItems.Remove(cartItem);

        public async Task SaveAsync() => await _context.SaveChangesAsync();
    }
}
