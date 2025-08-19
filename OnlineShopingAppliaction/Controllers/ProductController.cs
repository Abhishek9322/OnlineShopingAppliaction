using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OnlineShopingAppliaction.Controllers
{
    public class ProductController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;    
        public ProductController(ApplicationDbContext context , IWebHostEnvironment webHostEnvironment)
        { 
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        //Available Products / Filter category  /search bar by product name or the category name 

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new UnauthorizedAccessException("Invalid JWT Token");
            return int.Parse(userIdClaim.Value);
        }

        public async Task<IActionResult> Index(int? categoryId ,string searchQuery)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(searchQuery))
                products = products.Where(u => u.Name.Contains(searchQuery) || u.Category.Name.Contains(searchQuery));

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;

            return View(await products.ToListAsync());
        }




        //Product details + related products
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // ✅ Get all related products from same category except the current one
            var relatedProducts = await _context.Products
                                                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                                                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // Show Create Product Page with Category Dropdown
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        //Create Product with Image Upload and Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");

            if (product.Stock < 0)
                ModelState.AddModelError(nameof(product.Stock), "Stock cannot be negative.");

            product.OwnerId = GetCurrentUserId();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                product.ImagePath = "/uploads/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        //  Edit product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product =await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories =await _context.Categories.ToListAsync();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? ImageFile)
        {
            // Remove properties that are not posted or validated here
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");

            if (product == null) return BadRequest();

           
            if (product.Stock < 0)
            {
                ModelState.AddModelError(nameof(product.Stock), "Stock cannot be negative.");
            }

            var dbproduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (dbproduct == null) return NotFound();

            // ensure current admin owns the product
            var adminId = GetCurrentUserId();
            if (dbproduct.OwnerId != adminId) return Forbid();

            // If model validation failed, return the view showing errors
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                return View(product);
            }

            // Update the tracked entity (dbproduct) — DO NOT attach the incoming 'product' object
            dbproduct.Name = product.Name;
            dbproduct.Description = product.Description;
            dbproduct.Price = product.Price;
            dbproduct.Discount = product.Discount;
            dbproduct.CategoryId = product.CategoryId;
            dbproduct.Stock = product.Stock;

            // Handle image upload if provided
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // delete old image if exists
                if (!string.IsNullOrEmpty(dbproduct.ImagePath))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, dbproduct.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadDir, uniqueFileName);

                // async copy
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                dbproduct.ImagePath = "/uploads/" + uniqueFileName;
            }

            try
            {
                // dbproduct is already tracked; EF will pick up changes
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                // concurrency conflict handling
                ModelState.AddModelError("", "Unable to save changes. The product was modified by someone else. Please try again.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the product: " + ex.Message);
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }





        //  Delete product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                                         .Include(p => p.Category)
                                         .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            bool hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                TempData["ErrorMessage"] = "Cannot delete product. It is linked to existing orders.";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(product.ImagePath))
            {
                string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
