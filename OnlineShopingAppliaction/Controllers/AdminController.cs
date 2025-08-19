using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;

namespace OnlineShopingAppliaction.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }



        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            ViewBag.CurrentAdmin = User.Identity?.Name;
            var users = await _context.AppUsers.ToListAsync();
            return View(users);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            if (user.UserName == User.Identity?.Name)
            {
                TempData["Error"] = "Admin cannot delete himself";
                return RedirectToAction("Users");
            }

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User {user.UserName} has been deleted successfully.";
            return RedirectToAction("Users");
        }


    }
}
