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
        public async Task<IActionResult> Login(string email, string password)
        {
            ViewBag.Action = "login";
            if(ModelState.IsValid)
            {

            }
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
