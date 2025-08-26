using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Repository.Repository
{
    public class AppUserRepository : IAppUserRepository
    {
        private readonly ApplicationDbContext _context;
        public AppUserRepository(ApplicationDbContext context) 
        {
           _context = context;
        }
        public async Task AddUserAsync(AppUser user)
        {
            await _context.AppUsers.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<AppUser> GetByUsernameAsync(string username)
        {
            return await _context.AppUsers.FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.AppUsers.AnyAsync(u => u.UserName == username);
        }
    }
}
