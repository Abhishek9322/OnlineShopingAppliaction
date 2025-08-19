using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using System.Security.Claims;

namespace OnlineShopingAppliaction.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
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
            var user =await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
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
                var errors = ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();

                TempData["Error"] = "Validation Failed: " + string.Join(", ", errors);
                return View(model);
            }

            int userId = GetCurrentUserId();  // Get from JWT
            var user =await _context.AppUsers.FirstOrDefaultAsync(o => o.Id == userId);
            if (user == null)
            {
                TempData["Error"] = "User not found!";
                return View(model);
            }

            // Update fields
            user.UserName = model.UserName;
            user.Email = model.Email;

            // Update password only if provided
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            _context.AppUsers.Update(user);
           await  _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Edit");
        }
       
    }
}
