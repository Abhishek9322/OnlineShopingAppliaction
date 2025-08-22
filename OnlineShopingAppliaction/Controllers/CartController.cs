using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
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
        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrWhiteSpace(jwt))
            {
                TempData["ErrorMessage"] = "Please log in to update cart quantity.";
                return RedirectToAction("Login", "Account");
            }

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(x => x.Id == id);
            if (cartItem != null)
            {

                var product= await _context.Products.FirstOrDefaultAsync(x => x.Id==cartItem.ProductId);
                if (product != null &&cartItem.Quantity +1<=product.Stock) 
                {
                    cartItem.Quantity += 1;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["ErrorMEssage"] = "Cannot increase quantity .Not enough stock.";
                }
            }

            return RedirectToAction("Index");
        }


        //Decrease Quantity
        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                TempData["ErrorMessage"] = "Please log in to update quantity.";
                return RedirectToAction("Login", "Account");
            }

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(x => x.Id == id);
            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity -= 1;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }


        //cart 
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["LoginMessage"] = "Please log in to view your cart.";
                return RedirectToAction("Login", "Account");
            }

            int userId = GetCurrentUserId();
            var cart = await _context.CartItems
                                     .Include(c => c.Product)
                                     .Where(c => c.UserId == userId)
                                     .ToListAsync();

            // Calculate totals
            decimal total = 0;
            decimal totalDiscount = 0;
            decimal finalTotal = 0;

            foreach (var item in cart)
            {
                decimal productTotal = item.Product.Price * item.Quantity;
                decimal productDiscount = productTotal > DISCOUNT_THRESHOLD
                    ? productTotal * (DISCOUNT_PERCENT / 100)
                    : 0;

                total += productTotal;
                totalDiscount += productDiscount;
                finalTotal += productTotal - productDiscount;

                // Store discount & final total per product in ViewBag
                ViewData[$"Discount_{item.Id}"] = productDiscount;
                ViewData[$"Final_{item.Id}"] = productTotal - productDiscount;
            }

            ViewBag.Total = total;
            ViewBag.Discount = totalDiscount;
            ViewBag.FinalTotal = finalTotal;

            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            if (quantity < 1) quantity = 1;

            int userId = GetCurrentUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = $"Only {product.Stock} item(s) available for '{product.Name}'.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);

            if (existing != null)
            {
                if (existing.Quantity + quantity > product.Stock)
                {
                    TempData["ErrorMessage"] = $"Cannot add {quantity} more. Only {product.Stock - existing.Quantity} item(s) left.";
                    return RedirectToAction("Index");
                }
                existing.Quantity += quantity;
            }
            else
            {
                await _context.CartItems.AddAsync(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]  // allow GET request
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            int userId = GetCurrentUserId();
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Summary()
        {
            int userId = GetCurrentUserId();

            var cart = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            ComputeTotals(cart, out var total, out var totalDiscount, out var finalTotal);

            ViewBag.Total = total;
            ViewBag.Discount = totalDiscount;
            ViewBag.FinalTotal = finalTotal;

            return View(cart);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceSingleOrder(int productId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Please login to place order.";
                return RedirectToAction("Login", "Account");
            }

            if (quantity < 1) quantity = 1;

            int userId = GetCurrentUserId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = $"Not enough stock for {product.Name}. Available: {product.Stock}.";
                return RedirectToAction("Details", new { id = productId });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var lineTotal = product.Price * quantity;
                var lineDiscount = lineTotal > DISCOUNT_THRESHOLD ? lineTotal * (DISCOUNT_PERCENT / 100m) : 0m;
                var finalTotal = lineTotal - lineDiscount;


                //create a new order
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Subtotal = lineTotal,
                    Discount = lineDiscount,
                    Total = finalTotal,
                    Status = "Placed"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();


                //Create new oredritem row linked to that (order)
                var item = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = productId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    LineDiscount = lineDiscount,
                    LineTotal = finalTotal
                };
                _context.OrderItems.Add(item);


                //Reduce product stock
                product.Stock -= quantity;
                _context.Products.Update(product);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["SuccessMessage"] = $"Order placed for {product.Name} ({quantity}).";
                return RedirectToAction("Details", "Order", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ErrorMessage"] = $"Failed to place order: {ex.Message}";
                return RedirectToAction("Details", new { id = productId });
            }
        }


        // helper
        private static void ComputeTotals(
            List<CartItem> cart,
            out decimal total,
            out decimal totalDiscount,
            out decimal finalTotal)
        {
            total = 0m;
            totalDiscount = 0m;
            finalTotal = 0m;

            foreach (var item in cart)
            {
                var productTotal = item.Product.Price * item.Quantity;
                var productDiscount = productTotal > DISCOUNT_THRESHOLD ? productTotal * (DISCOUNT_PERCENT / 100m) : 0m;

                total += productTotal;
                totalDiscount += productDiscount;
                finalTotal += productTotal - productDiscount;
            }
        }


    }
}
