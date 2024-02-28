using HelpDeskAI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskAI.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            ViewBag.Action = "login";
            return View();;
        }
        public IActionResult Register()
        {
            ViewBag.Action = "register";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string pass)
        {
            ViewBag.Action = "login";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User Model)
        {
            ViewBag.Action = "register";
            return View();
        }
    }
}
