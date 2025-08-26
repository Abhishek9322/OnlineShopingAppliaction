using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;
using OnlineShopingAppliaction.Service;
using System.Timers;

namespace OnlineShopingAppliaction.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAppUserRepository _userRepository;
        private readonly JwtService _jwt;

        public AccountController(IAppUserRepository userRepository, JwtService jwt)
        {
            _userRepository = userRepository;
            _jwt = jwt;
        }

        //Registration View
        public IActionResult Register() => View();


        [HttpPost]
        public async Task<IActionResult> Register(AppUser user)
        {
            if (await _userRepository.UsernameExistsAsync(user.UserName))
            {
                ModelState.AddModelError("", "Username already exists");
                return View(user);
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "Password is required.");
                return View(user);
            }

            // Hash password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            // Default role if not provided
            if (string.IsNullOrEmpty(user.Role))
                user.Role = "User";
            else
            {
                var allowedRoles = new[] { "Admin", "User", "DeliveryBoy" };
                if (!allowedRoles.Contains(user.Role))
                {
                    ModelState.AddModelError("Role", "Invalid role selected");
                    return View(user);
                }
            }

            await _userRepository.AddUserAsync(user);

            return RedirectToAction("Login");

        }

        // Login View
        public IActionResult Login() => View();


        [HttpPost]
        public  async Task<IActionResult> Login(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid credentials";
                return View();
            }

            // Generate JWT token and store in cookie
            var token = _jwt.GenerateToken(user);
            Response.Cookies.Append("jwt", token, new CookieOptions { HttpOnly = true });

            return RedirectToAction("Index", "Home");
        }

        //  Logout
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login");
        }




    }
}
