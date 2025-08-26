using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface IHomeRepository
    {
        Task<List<Category>> GetCategoriesWithProductsAsync();
    }
}
