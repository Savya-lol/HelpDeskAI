using HelpDeskAI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskAI.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }

        public async Task<IActionResult> Login(string email, string pass)
        {
            return View();
        }

        public async Task<IActionResult> Register(User Model)
        {
            return View();
        }
    }
}
