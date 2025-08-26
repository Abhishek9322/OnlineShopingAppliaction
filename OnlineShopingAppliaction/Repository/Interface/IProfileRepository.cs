using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface IProfileRepository
    {
        Task<AppUser?> GetUserByIdAsync(int id);
        Task UpdateUserAsync(AppUser user);
        Task SaveAsync();
    }
}
