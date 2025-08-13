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
        public IActionResult Index(int? categoryId ,string searchQuery)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(searchQuery))
                products = products.Where(u => u.Name.Contains(searchQuery) || u.Category.Name.Contains(searchQuery));

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;


            return View(products.ToList());
        }




        //Product details + related products
        public IActionResult Details(int id)
        {
            var product = _context.Products.Include(p => p.Category).SingleOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            var relatedProducts = _context.Products
                                          .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                                          .Take(4)
                                          .ToList();

            ViewBag.RelatedProducts = relatedProducts;
            return View(product);
        }


        // Show Create Product Page with Category Dropdown
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        //Create Product with Image Upload and Category
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product product, IFormFile ImageFile)
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");


            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                product.ImagePath = "/uploads/" + uniqueFileName;
              //  ModelState.Remove("ImagePath");
            }

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);

        }

        //  Edit product
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(Product product ,IFormFile ImageFile)
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Category");

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                product.ImagePath = "/uploads/" + uniqueFileName;
               // ModelState.Remove("ImagePath");
            }

            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }






        //  Delete product
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
