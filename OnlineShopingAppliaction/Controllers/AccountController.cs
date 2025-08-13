using Microsoft.AspNetCore.Mvc;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Service;

namespace OnlineShopingAppliaction.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;

        public AccountController(ApplicationDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        //Registration View
        public IActionResult Register() => View();


        [HttpPost]
        public IActionResult Register(AppUser user)
        {
            if (_context.AppUsers.Any(u => u.UserName == user.UserName))
            {
                ModelState.AddModelError("", "Username already exists");
                return View(user);
            }

            if(string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError("Passwordhash", "Password is required.");
            }


            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            if(string.IsNullOrEmpty(user.Role))
                  user.Role = "User"; // Default role

            _context.AppUsers.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");


        }

        // Login View
        public IActionResult Login() => View();


        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.AppUsers.FirstOrDefault(u => u.UserName == username);
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
