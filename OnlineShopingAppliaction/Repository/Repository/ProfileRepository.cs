using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Repository.Repository
{
    public class ProfileRepository: IProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public ProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> GetUserByIdAsync(int id)
        {
            return await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task UpdateUserAsync(AppUser user)
        {
            _context.AppUsers.Update(user);
            await Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}
