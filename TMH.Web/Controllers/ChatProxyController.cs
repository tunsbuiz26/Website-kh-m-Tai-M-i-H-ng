using Microsoft.AspNetCore.Mvc;

namespace TMH.Web.Controllers
{
    public class ChatProxyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
