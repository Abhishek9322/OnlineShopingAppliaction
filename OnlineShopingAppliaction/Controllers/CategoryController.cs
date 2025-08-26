using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;

        public CategoryController(ICategoryRepository categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }
        //List category
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepo.GetAllAsync();
            return View(categories);
        }

        //Create category
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepo.AddAsync(category);
                await _categoryRepo.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }


        //Edit Category
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                await _categoryRepo.UpdateAsync(category);
                await _categoryRepo.SaveAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }
        // Delete Category
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category != null)
            {
                await _categoryRepo.DeleteAsync(category);
                await _categoryRepo.SaveAsync();
            }
            return RedirectToAction(nameof(Index));
        }



    }
}
