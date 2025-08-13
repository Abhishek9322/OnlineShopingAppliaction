using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using System.Security.Claims;

namespace OnlineShopingAppliaction.Controllers
{
    
    public class CartController : Controller
    {

        private readonly ApplicationDbContext _context;
        private const decimal DISCOUNT_THRESHOLD = 5000;
        private const decimal DISCOUNT_PERCENT = 10;

        public CartController(ApplicationDbContext context) { _context = context; }

        
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new UnauthorizedAccessException("Invalid JWT Token");
            return int.Parse(userIdClaim.Value);
        }



        //Quentity addign
        public IActionResult IncreaseQuantity(int id)
        {
            var jwt = Request.Cookies["jwt"];
            if(string.IsNullOrWhiteSpace(jwt))
            {
                TempData["ErrorMessage"] = "Please log in to update cart Quantity. ";
                return RedirectToAction("Login", "Account");
            }


            var cartItem=_context.CartItems.FirstOrDefault(x => x.Id == id);
            if(cartItem != null)
            {
                 cartItem.Quantity += 1;
                    _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        //Decrease Quantity
        public IActionResult DecreaseQuantity(int id)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                TempData["ErrorMessage"] = "Plase loh in to update Quentity .";
                return RedirectToAction("Login", "Account");
            }

            var caretItem = _context.CartItems.FirstOrDefault(x => x.Id == id);
            if (caretItem != null)
            {
                if (caretItem.Quantity > 1)
                {
                    caretItem.Quantity -= 1;
                    _context.SaveChanges();
                }
                else
                {
                    _context.CartItems.Remove(caretItem);
                    _context.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }



        //cart 
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["LoginMessage"] = "Please log in to view your cart.";
                return RedirectToAction("Login", "Account");
            }


            int userId = GetCurrentUserId();
            var cart = _context.CartItems.Include(c => c.Product)
                                         .Where(c => c.UserId == userId)
                                         .ToList();

            // Calculate totals
            decimal total = 0;
            decimal totalDiscount = 0;
            decimal finalTotal = 0;

            foreach (var item in cart)
            {
                decimal productTotal = item.Product.Price * item.Quantity;
                decimal productDiscount = productTotal > DISCOUNT_THRESHOLD ? productTotal * (DISCOUNT_PERCENT / 100) : 0;

                total += productTotal;
                totalDiscount += productDiscount;
                finalTotal += (productTotal - productDiscount);

                //  Store discount & final total per product in ViewBag dynamically
                ViewData[$"Discount_{item.Id}"] = productDiscount;
                ViewData[$"Final_{item.Id}"] = productTotal - productDiscount;
            }

            ViewBag.Total = total;
            ViewBag.Discount = totalDiscount;
            ViewBag.FinalTotal = finalTotal;

            return View(cart);
        }

        public IActionResult AddToCart(int productId)
        {

            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            int userId = GetCurrentUserId();

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            var existing = _context.CartItems.FirstOrDefault(c => c.ProductId == productId && c.UserId == userId);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name, 
                    Quantity = 1,
                    UserId = userId
                });
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }


        public IActionResult RemoveFromCart(int id)
        {
            int userId = GetCurrentUserId();
            var item = _context.CartItems.FirstOrDefault(c => c.Id == id && c.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Summary()
        {
            int userId = GetCurrentUserId();
            var cart = _context.CartItems.Include(c => c.Product)
                                         .Where(c => c.UserId == userId) //  Filter user items
                                         .ToList();

            return View(cart);
        }
    }
}
