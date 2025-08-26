using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(int? categoryId, string? searchQuery);
        Task<Product?> GetByIdAsync(int id);
        Task<IEnumerable<Product>> GetRelatedAsync(int categoryId, int excludeId);
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task<bool> HasOrdersAsync(int productId);
    }
}
