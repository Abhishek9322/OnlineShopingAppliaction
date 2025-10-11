using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShopingAppliaction.Models;
using OnlineShopingAppliaction.Repository.Interface;
using OnlineShopingAppliaction.Service;
using System.Diagnostics;

namespace OnlineShopingAppliaction.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepo;

        private readonly ApiService _apiService;
        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepo,ApiService apiService)
        {
            _logger = logger;
            _homeRepo = homeRepo;
            _apiService = apiService;
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

        public IActionResult ChatAi()
        {
            return View(SendMessage);
        }
        [HttpPost]
        [Route("Chat/SendMessage")]
        [IgnoreAntiforgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SendMessage(string message, string history = "")
        {
            _logger.LogInformation("SendMessage called with: {Message}", message);

            if (string.IsNullOrWhiteSpace(message))
                return Json(new { error = "Message cannot be empty." });

            try
            {
                var response = await _apiService.GetChatResponceAsync(message, history);

                if (string.IsNullOrEmpty(response))
                    return Json(new { error = "No response from AI service." });

                return Json(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI chat error: {Message}", message);
                return Json(new { error = "Chat service error. " + ex.Message });
            }
        }













        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
