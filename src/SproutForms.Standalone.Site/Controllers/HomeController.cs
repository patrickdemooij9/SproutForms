using Microsoft.AspNetCore.Mvc;

namespace SproutForms.Standalone.Site.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
