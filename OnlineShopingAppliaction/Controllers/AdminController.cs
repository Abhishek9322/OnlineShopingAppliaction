using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Users()
        {
            ViewBag.CurrentAdmin = User.Identity?.Name;
            var users = _context.AppUsers.ToList();
            return View(users);
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.AppUsers.Find(id);
            if (user == null) return NotFound();

            if (user.UserName == User.Identity?.Name)        
            {
                TempData["Error"] = "Admin cannot delete himself";
                return RedirectToAction("Users");
            }

            _context.AppUsers.Remove(user);
            _context.SaveChanges();


            TempData["Success"] = $" User {user.UserName} has been deleted successfully.";
            return RedirectToAction("Users");
        }



    }
}
