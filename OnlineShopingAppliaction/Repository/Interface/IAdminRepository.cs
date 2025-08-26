using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface IAdminRepository
    {
        Task<List<AppUser>> GetAllUsersAsync();
        Task<AppUser> GetByIdAsync(int id);
        Task DeleteUserAsync(AppUser user);
    }
}
