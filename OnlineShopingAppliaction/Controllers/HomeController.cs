using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShopingAppliaction.Data;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;

namespace OnlineShopingAppliaction.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepo;

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepo)
        {
            _logger = logger;
            _homeRepo = homeRepo;
        }

        public async Task<IActionResult> Index()
        {
            var categoriesWithProducts = await _homeRepo.GetCategoriesWithProductsAsync();
            return View(categoriesWithProducts);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
