using Elysian.Models;
using Elysian.Data;  
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Elysian.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db; 

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
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

        public IActionResult Shop(string category)
        {
            var products = _db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                var cat = category.ToLower();
                products = products.Where(p => p.Category.ToLower().Contains(cat));
            }

            return View(products.ToList());
        }

    }
}
