using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetMyOrdersAsync(int userId);
        Task<IEnumerable<OrderItem>> GetAdminItemsAsync(int adminId);
        Task AddOrderAsync(Order order);
        Task AddOrderItemsAsync(IEnumerable<OrderItem> items);
        Task UpdateOrderAsync(Order order);
        Task RemoveOrderAsync(Order order);
        Task<IEnumerable<AppUser>> GetDeliveryBoysAsync();
        Task SaveChangesAsync();
    }
}
