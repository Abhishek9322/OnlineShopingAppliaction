using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using System.Security.Claims;

namespace OnlineShopingAppliaction.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context) { _context = context; }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new UnauthorizedAccessException("Invalid JWT Token");
            return int.Parse(userIdClaim.Value);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            int userId = GetCurrentUserId();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // check stock first
            var insufficient = cartItems.FirstOrDefault(ci => ci.Product.Stock < ci.Quantity);
            if (insufficient != null)
            {
                TempData["Error"] = $"Not enough stock for {insufficient.Product.Name}";
                return RedirectToAction("Index", "Cart");
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Subtotal = cartItems.Sum(c => c.Product.Price * c.Quantity),
                    Discount = 0m, // compute if needed
                    Total = cartItems.Sum(c => c.Product.Price * c.Quantity) // adjust if discount logic exists
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var cartItem in cartItems)
                {
                    await _context.OrderItems.AddAsync(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Product.Name,
                        UnitPrice = cartItem.Product.Price,
                        Quantity = cartItem.Quantity,
                        LineDiscount = 0m,
                        LineTotal = cartItem.Product.Price * cartItem.Quantity
                    });

                    cartItem.Product.Stock -= cartItem.Quantity;
                    _context.Products.Update(cartItem.Product);
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction(nameof(My));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Failed to place order: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }


        [HttpPost]
        public async Task<IActionResult> PlaceSingleOrder(int cartItemId)
        {
            int userId = GetCurrentUserId();

            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Cart item not found.";
                return RedirectToAction("Index", "Cart");
            }

            if (cartItem.Product.Stock < cartItem.Quantity)
            {
                TempData["Error"] = "This product is out of stock.";
                return RedirectToAction("Index", "Cart");
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Subtotal = cartItem.Product.Price * cartItem.Quantity,
                    Discount = 0m,
                    Total = cartItem.Product.Price * cartItem.Quantity
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                await _context.OrderItems.AddAsync(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    UnitPrice = cartItem.Product.Price,
                    Quantity = cartItem.Quantity,
                    LineDiscount = 0m,
                    LineTotal = cartItem.Product.Price * cartItem.Quantity
                });

                //Remove product from cart after placing oredr

                cartItem.Product.Stock -= cartItem.Quantity;
                _context.Products.Update(cartItem.Product);

                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction(nameof(My));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Failed to place order: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }


        //cancle oredr
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return NotFound();
            }

            // Change status instead of deleting
            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToAction("My"); // go back to orders list
        }

        //After the canccel order 
        [HttpPost]
        public async Task<IActionResult> RemoveOrder(int orderId)
        {
            var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                // Remove related order items first (if using EF with cascade delete, this might not be needed)
                var items = _context.OrderItems.Where(i => i.OrderId == orderId);
                _context.OrderItems.RemoveRange(items);

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            // Redirect back to My Orders page
            return RedirectToAction("My");
        }





        // 
        [HttpGet]
        public async Task<IActionResult> Checkout(int? cartItemId)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                TempData["ErrorMessage"] = "Please log in to update quantity.";
                return RedirectToAction("Login", "Account");
            }

            int userId = GetCurrentUserId();

            var vm = new CheckoutViewModel();
            vm.CartItemId = cartItemId;

            List<CartItem> items;
            if (cartItemId.HasValue)
            {
                var ci = await _context.CartItems
                                       .Include(c => c.Product)
                                       .FirstOrDefaultAsync(c => c.Id == cartItemId.Value && c.UserId == userId);
                if (ci == null)
                {
                    TempData["Error"] = "Cart item not found.";
                    return RedirectToAction("Index", "Cart");
                }
                items = new List<CartItem> { ci };
            }
            else
            {
                items = await _context.CartItems
                                      .Include(c => c.Product)
                                      .Where(c => c.UserId == userId)
                                      .ToListAsync();
                if (!items.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // compute totals same as your other helper
            const decimal DISCOUNT_THRESHOLD = 5000m;
            const decimal DISCOUNT_PERCENT = 10m;

            decimal subtotal = 0m, discount = 0m, finalTotal = 0m;
            foreach (var it in items)
            {
                var line = it.Product.Price * it.Quantity;
                var lineDiscount = line > DISCOUNT_THRESHOLD ? line * (DISCOUNT_PERCENT / 100m) : 0m;
                subtotal += line;
                discount += lineDiscount;
                finalTotal += line - lineDiscount;
            }

            vm.Items = items;
            vm.Subtotal = subtotal;
            vm.Discount = discount;
            vm.FinalTotal = finalTotal;

            // Optionally prefill shipping from user's profile if you have one
            // var user = _context.Users.Find(userId); vm.ShippingFullName = user?.FullName ?? "";

            return View(vm);
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {


            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                TempData["ErrorMessage"] = "Please log in to update quantity.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                // re-populate items and totals for show
                return await Checkout(model.CartItemId);
            }

            int userId = GetCurrentUserId();

            // re-fetch items inside transaction and check stock
            List<CartItem> items;
            if (model.CartItemId.HasValue)
            {
                var ci = await _context.CartItems.Include(c => c.Product)
                         .FirstOrDefaultAsync(c => c.Id == model.CartItemId.Value && c.UserId == userId);
                if (ci == null)
                {
                    TempData["Error"] = "Cart item not found.";
                    return RedirectToAction("Index", "Cart");
                }
                items = new List<CartItem> { ci };
            }
            else
            {
                items = await _context.CartItems
                                       .Include(c => c.Product)
                                       .Where(c => c.UserId == userId)
                                       .ToListAsync();

                if (!items.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // compute totals & re-check stock
            const decimal DISCOUNT_THRESHOLD = 5000m;
            const decimal DISCOUNT_PERCENT = 10m;

            decimal subtotal = 0m, discount = 0m, finalTotal = 0m;
            foreach (var it in items)
            {
                if (it.Product.Stock < it.Quantity)
                {
                    TempData["Error"] = $"Insufficient stock for {it.Product.Name}.";
                    return RedirectToAction("Index", "Cart");
                }
                var line = it.Product.Price * it.Quantity;
                var lineDiscount = line > DISCOUNT_THRESHOLD ? line * (DISCOUNT_PERCENT / 100m) : 0m;
                subtotal += line;
                discount += lineDiscount;
                finalTotal += line - lineDiscount;
            }

            await using var OrderTransactionStart = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Subtotal = subtotal,
                    Discount = discount,
                    Total = finalTotal,
                    Status = "Placed",

                    // shipping
                    ShippingFullName = model.ShippingFullName,
                    ShippingPhone = model.ShippingPhone,
                    ShippingAddressLine1 = model.ShippingAddressLine1,
                    ShippingAddressLine2 = model.ShippingAddressLine2,
                    ShippingCity = model.ShippingCity,
                    ShippingState = model.ShippingState,
                    ShippingCountry = model.ShippingCountry,
                    ShippingPincode = model.ShippingPincode,
                    ShippingNotes = model.ShippingNotes
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var it in items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);
                    if (product == null)
                        throw new InvalidOperationException("Product disappeared.");

                    if (product.Stock < it.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for {product.Name}.");

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = it.ProductId,
                        ProductName = it.Product.Name,
                        UnitPrice = it.Product.Price,
                        Quantity = it.Quantity,
                        LineDiscount = it.Product.Price * it.Quantity > DISCOUNT_THRESHOLD ? it.Product.Price * it.Quantity * (DISCOUNT_PERCENT / 100m) : 0m,
                        LineTotal = it.Product.Price * it.Quantity - (it.Product.Price * it.Quantity > DISCOUNT_THRESHOLD ? it.Product.Price * it.Quantity * (DISCOUNT_PERCENT / 100m) : 0m)
                    };
                    _context.OrderItems.Add(orderItem);

                    // reduce stock
                    product.Stock -= it.Quantity;
                    _context.Products.Update(product);

                    // remove cart item
                    _context.CartItems.Remove(it);
                }

                await _context.SaveChangesAsync();
                await OrderTransactionStart.CommitAsync();

                TempData["Success"] = "Order placed successfully.";
                return RedirectToAction("Details", new { id = order.Id });
            }
            catch (Exception ex)
            {
                await OrderTransactionStart.RollbackAsync();
                TempData["Error"] = "Failed to place order: " + ex.Message;
                return RedirectToAction("Index", "Cart");
            }
        }





        //  My orders
        public async Task<IActionResult> My()
        {
            int userId = GetCurrentUserId();
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        //  Order details 
        public async Task<IActionResult> Details(int id)
        {
            int userId = GetCurrentUserId();
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.UserId != userId && !User.IsInRole("Admin")) return Forbid();
            return View(order);
        }

        //  Admin: see items that include my products
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminItems()
        {

            int adminId = GetCurrentUserId();

            var items = await _context.OrderItems
                .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.OwnerId == adminId)
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .ToListAsync();

            return View(items);
        }


        // [Authorize(Roles ="Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();


            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order status updated successfully !";
            return RedirectToAction(nameof(AdminItems));

        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AssignOrderToDeliveryBoy(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(p => p.Id == orderId);
            if (order == null) return NotFound();

            var deliveryBoys = await _context.AppUsers
                .Where(u => u.Role == "DeliveryBoy")
                .ToListAsync();


            ViewBag.DeliveryBoy = deliveryBoys;
            return View(order);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AssignOrderToDeliveryBoy(int orderId, int deliveryBoyId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.DeliveryBoyId = deliveryBoyId;
            order.Status = "Shipped";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order assigned successfully!";
            return RedirectToAction(nameof(AdminItems));
        }


        [Authorize(Roles = "DeliveryBoy")]
        [HttpGet]
        public async Task<IActionResult> MyAssignedOrders()
        {
            int deliveryBoyId = GetCurrentUserId(); // your method to get current delivery boy ID

            // Fetch all assigned orders including delivered ones
            var orders = await _context.Orders
                .Where(o => o.DeliveryBoyId == deliveryBoyId)  // Do not filter out delivered
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [Authorize(Roles = "DeliveryBoy")]
        [HttpPost]
        public async Task<IActionResult> MarkOrderDelivered(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            int deliveryBoyId = GetCurrentUserId();
            if (order.DeliveryBoyId != deliveryBoyId)
                return Forbid();

            order.Status = "Delivered";
            await _context.SaveChangesAsync();

            // Redirect back to the same page
            return RedirectToAction(nameof(MyAssignedOrders));
        }

    }
}
