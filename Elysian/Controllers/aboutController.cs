using Microsoft.AspNetCore.Mvc;

namespace Elysian.Controllers
{
    public class aboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
