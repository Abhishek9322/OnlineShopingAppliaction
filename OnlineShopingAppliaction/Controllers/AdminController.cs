using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;

        public AdminController(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }



        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            ViewBag.CurrentAdmin = User.Identity?.Name;
            var users = await _adminRepository.GetAllUsersAsync();
            return View(users);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _adminRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (user.UserName == User.Identity?.Name)
            {
                TempData["Error"] = "Admin cannot delete himself";
                return RedirectToAction("Users");
            }

            await _adminRepository.DeleteUserAsync(user);

            TempData["Success"] = $"User {user.UserName} has been deleted successfully.";
            return RedirectToAction("Users");
        }


    }
}
