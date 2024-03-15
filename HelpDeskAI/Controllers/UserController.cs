using HelpDeskAI.Models.Auth;
using HelpDeskAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskAI.Controllers
{
    public class UserController : Controller
    {
        private readonly UserDataAccess _userDataAccess;
        public UserController(UserDataAccess userDataAccess)
        {
            _userDataAccess = userDataAccess;
        }
        public IActionResult Profile(string email)
        {
           User model = _userDataAccess.GetUserByEmail(email);
            return View(model:model);
        }
    }
}
