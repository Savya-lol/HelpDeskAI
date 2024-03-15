using HelpDeskAI.Models;
using HelpDeskAI.Models.Chat;
using HelpDeskAI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HelpDeskAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ChatService _chatService;
        public HomeController(ILogger<HomeController> logger, ChatService chatService)
        {
            _logger = logger;
            _chatService=chatService;
        }

        public IActionResult Index()
        {
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

        public void Chat(Room model,string message)
        {
           if(model == null)
            {
                if(model.isAIassisted)
                {

                }
            }
        }
    }
}
