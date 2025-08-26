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
using OnlineShopingAppliaction.Repository.Interface;


namespace OnlineShopingAppliaction.Controllers
{
    public class ProductController : Controller
    {

        private readonly IProductRepository _productRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;    
        
        public ProductController(IProductRepository productRepository, IWebHostEnvironment webHostEnvironment)
        {
            _productRepo = productRepository;
            _webHostEnvironment = webHostEnvironment;
            
        }

        //Available Products / Filter category  /search bar by product name or the category name 

        private int GetCurrentUserId() =>
              int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());


        public async Task<IActionResult> Index(int? categoryId ,string searchQuery)
        {
            var products = await _productRepo.GetAllAsync(categoryId, searchQuery);
            ViewBag.Categories = await _productRepo.GetCategoriesAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;
            return View(products);
        }




        //Product details + related products
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            ViewBag.RelatedProducts = await _productRepo.GetRelatedAsync(product.CategoryId, product.Id);
            return View(product);
        }

        // Show Create Product Page with Category Dropdown
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _productRepo.GetCategoriesAsync();
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

            // ✅ Handle image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ImageFile.CopyToAsync(stream);

                product.ImagePath = "/uploads/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                await _productRepo.AddAsync(product);
                return RedirectToAction("Index");
            }

            ViewBag.Categories = await _productRepo.GetCategoriesAsync();
            return View(product);
        }

        // Edit Product (GET)
      

        //  Edit product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = await _productRepo.GetCategoriesAsync();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? ImageFile)
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");
            ModelState.Remove("Owner");

            if (product == null) return BadRequest();
            if (product.Stock < 0)
                ModelState.AddModelError(nameof(product.Stock), "Stock cannot be negative.");

            var dbproduct = await _productRepo.GetByIdAsync(product.Id);
            if (dbproduct == null) return NotFound();

            var adminId = GetCurrentUserId();
            if (dbproduct.OwnerId != adminId) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _productRepo.GetCategoriesAsync();
                return View(product);
            }

            // Update fields
            dbproduct.Name = product.Name;
            dbproduct.Description = product.Description;
            dbproduct.Price = product.Price;
            dbproduct.Discount = product.Discount;
            dbproduct.CategoryId = product.CategoryId;
            dbproduct.Stock = product.Stock;

            // Handle image replacement
            if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(dbproduct.ImagePath))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, dbproduct.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ImageFile.CopyToAsync(stream);

                dbproduct.ImagePath = "/uploads/" + uniqueFileName;
            }

            try
            {
                await _productRepo.UpdateAsync(dbproduct);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error while updating: " + ex.Message);
            }

            ViewBag.Categories = await _productRepo.GetCategoriesAsync();
            return View(product);
        }





        //  Delete product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            if (await _productRepo.HasOrdersAsync(id))
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

            await _productRepo.DeleteAsync(product);
            return RedirectToAction("Index");
        }
    }
}
