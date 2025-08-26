using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Repository.Repository
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        public AdminRepository(ApplicationDbContext context) 
        {
            _context = context;
        }
        public async Task DeleteUserAsync(AppUser user)
        {
            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            return await _context.AppUsers.ToListAsync();
        }

        public async Task<AppUser> GetByIdAsync(int id)
        {
            return await _context.AppUsers.FindAsync(id);
        }
    }
}
