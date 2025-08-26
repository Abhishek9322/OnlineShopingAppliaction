using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;
using System.Security.Claims;

namespace OnlineShopingAppliaction.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileRepository _profileRepo;

        public ProfileController(IProfileRepository profileRepo)
        {
            _profileRepo = profileRepo;
        }



        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                TempData["Error"] = "Unauthorized Access. Please login again.";
                Response.Redirect("/Account/Login");
                return 0;
            }
            return int.Parse(userIdClaim.Value);
        }

        //  Show Profile Update Form
        public async Task<IActionResult> Edit()
        {
            int userId = GetCurrentUserId();
            var user = await _profileRepo.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            var vm = new ProfileUpdateViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };
            return View(vm);
        }

        //  Update Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileUpdateViewModel model)
        {  // Debug ModelState Errors
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation Failed";
                return View(model);
            }

            int userId = GetCurrentUserId();
            var user = await _profileRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found!";
                return View(model);
            }

            user.UserName = model.UserName;
            user.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            await _profileRepo.UpdateUserAsync(user);
            await _profileRepo.SaveAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Edit");
        }

    }
}
