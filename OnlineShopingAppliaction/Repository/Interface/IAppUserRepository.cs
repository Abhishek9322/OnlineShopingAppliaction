using OnlineShopingAppliaction.Models;

namespace OnlineShopingAppliaction.Repository.Interface
{
    public interface IAppUserRepository
    {
        Task<AppUser> GetByUsernameAsync(string username);
        Task<bool> UsernameExistsAsync(string username);
        Task AddUserAsync(AppUser user);
    }
}
