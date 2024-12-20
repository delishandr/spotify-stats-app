using Microsoft.AspNetCore.Mvc;
using spotify_stats_app.Models;
using System.Diagnostics;

namespace spotify_stats_app.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            JsonFile file = new JsonFile();
            return View(file);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Tutorial()
        {
            ViewBag.Title = "Tutorial";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
