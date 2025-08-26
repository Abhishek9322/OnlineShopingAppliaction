using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;
using OnlineShopingAppliaction.Repository.Repository;
using System.Security.Claims;

namespace OnlineShopingAppliaction.Controllers
{

    public class CartController : Controller
    {

        private readonly ICartRepository _cartRepo;
        private readonly IProductRepository _productRepo;
        private readonly IOrderRepository _orderRepo;


        private const decimal DISCOUNT_THRESHOLD = 5000;
        private const decimal DISCOUNT_PERCENT = 10;
        public CartController(ICartRepository cartRepo, IProductRepository productRepo, IOrderRepository orderRepo)
        {
            _cartRepo = cartRepo;
            _productRepo = productRepo;
            _orderRepo = orderRepo;
        }


        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new UnauthorizedAccessException("Invalid JWT Token");
            return int.Parse(userIdClaim.Value);
        }



        //Quentity addign
        public async Task<IActionResult> IncreaseQuantity(int id)
        {
            var cartItem = await _cartRepo.GetCartItemByIdAsync(id);
            if (cartItem != null)
            {
                var product = await _productRepo.GetByIdAsync(cartItem.ProductId);
                if (product != null && cartItem.Quantity + 1 <= product.Stock)
                {
                    cartItem.Quantity++;
                    await _cartRepo.SaveAsync();
                }
                else
                {
                    TempData["ErrorMessage"] = "Cannot increase quantity. Not enough stock.";
                }
            }

            return RedirectToAction("Index");
        }


        //Decrease Quantity
        public async Task<IActionResult> DecreaseQuantity(int id)
        {
            var cartItem = await _cartRepo.GetCartItemByIdAsync(id);
            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                    await _cartRepo.SaveAsync();
                }
                else
                {
                    await _cartRepo.RemoveCartItemAsync(cartItem);
                    await _cartRepo.SaveAsync();
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
            var cart = await _cartRepo.GetUserCartAsync(userId);

            ComputeTotals(cart, out var total, out var discount, out var final);

            ViewBag.Total = total;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = final;

            return View(cart);
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            if (quantity < 1) quantity = 1;

            int userId = GetCurrentUserId();
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null) return NotFound();

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = $"Only {product.Stock} left for {product.Name}.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var existing = await _cartRepo.GetUserCartItemAsync(userId, productId);
            if (existing != null)
            {
                if (existing.Quantity + quantity > product.Stock)
                {
                    TempData["ErrorMessage"] = $"Not enough stock.";
                    return RedirectToAction("Index");
                }
                existing.Quantity += quantity;
            }
            else
            {
                await _cartRepo.AddCartItemAsync(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UserId = userId
                });
            }

            await _cartRepo.SaveAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]  // allow GET request
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            int userId = GetCurrentUserId();
            var item = await _cartRepo.GetCartItemByIdAsync(id);
            if (item != null && item.UserId == userId)
            {
                await _cartRepo.RemoveCartItemAsync(item);
                await _cartRepo.SaveAsync();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Summary()
        {
            int userId = GetCurrentUserId();

            var cart = await _cartRepo.GetUserCartAsync(userId);

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

            int userId = GetCurrentUserId();
            var product = await _productRepo.GetByIdAsync(productId);
            if (product == null) return NotFound();

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = "Not enough stock.";
                return RedirectToAction("Details", new { id = productId });
            }

            var lineTotal = product.Price * quantity;
            var lineDiscount = lineTotal > DISCOUNT_THRESHOLD ? lineTotal * (DISCOUNT_PERCENT / 100m) : 0m;
            var finalTotal = lineTotal - lineDiscount;

            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Subtotal = lineTotal,
                Discount = lineDiscount,
                Total = finalTotal,
                Status = "Placed"
            };

            await _orderRepo.AddOrderAsync(order);
            await _orderRepo.SaveChangesAsync();

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

            await _orderRepo.AddOrderItemsAsync(new List<OrderItem> { item });
            await _orderRepo.SaveChangesAsync();

            product.Stock -= quantity;
            await _productRepo.UpdateAsync(product);

            TempData["SuccessMessage"] = $"Order placed for {product.Name} ({quantity}).";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }


        // helper
        private static void ComputeTotals(List<CartItem> cart, out decimal total, out decimal discount, out decimal final)
        {
            total = discount = final = 0;
            foreach (var item in cart)
            {
                var productTotal = item.Product.Price * item.Quantity;
                var productDiscount = productTotal > DISCOUNT_THRESHOLD ? productTotal * (DISCOUNT_PERCENT / 100m) : 0m;

                total += productTotal;
                discount += productDiscount;
                final += productTotal - productDiscount;
            }


        }
    }
}
