using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Repository.Repository
{
    public class OrderRepository:IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderRepository(ApplicationDbContext context) { _context = context; }

        public async Task<Order?> GetByIdAsync(int id) =>
            await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<IEnumerable<Order>> GetMyOrdersAsync(int userId) =>
            await _context.Orders.Where(o => o.UserId == userId)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<OrderItem>> GetAdminItemsAsync(int adminId) =>
            await _context.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.OwnerId == adminId)
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .ToListAsync();

        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task AddOrderItemsAsync(IEnumerable<OrderItem> items)
        {
            await _context.OrderItems.AddRangeAsync(items);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
        }

        public async Task RemoveOrderAsync(Order order)
        {
            _context.Orders.Remove(order);
        }

        public async Task<IEnumerable<AppUser>> GetDeliveryBoysAsync() =>
            await _context.AppUsers.Where(u => u.Role == "DeliveryBoy").ToListAsync();

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    }
}
