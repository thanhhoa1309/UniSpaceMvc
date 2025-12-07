using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UniSpace.MVC.Presentation.Models;

namespace UniSpace.MVC.Presentation.Controllers
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
            // Ki?m tra xem user ?ã ??ng nh?p ch?a
            var token = HttpContext.Session.GetString("AuthToken");
            ViewBag.IsAuthenticated = !string.IsNullOrEmpty(token);
            
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
