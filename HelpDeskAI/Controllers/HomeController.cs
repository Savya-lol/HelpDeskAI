using HelpDeskAI.Models;
using HelpDeskAI.Models.Chat;
using HelpDeskAI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace HelpDeskAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ChatHub _chatService;
        private readonly UserDataAccess _userDataAccess;
        public HomeController(ILogger<HomeController> logger, ChatHub chatService, UserDataAccess userDataAccess)
        {
            _logger = logger;
            _chatService = chatService;
            _userDataAccess = userDataAccess;
        }

        [Authorize]
        public async Task<IActionResult> Index(Room model)
        {
            Debug.WriteLine(User.FindFirstValue(ClaimTypes.Email));
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
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
