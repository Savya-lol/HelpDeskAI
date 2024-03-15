using HelpDeskAI.Models.Auth;
using HelpDeskAI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;
using System.Web.Helpers;

namespace HelpDeskAI.Controllers
{
    public class AuthController : Controller
    {

        private readonly UserDataAccess _userDataAccess;
        private readonly MailService _mailService;

        public AuthController(UserDataAccess userDataAccess, MailService mailService)
        {
            _userDataAccess = userDataAccess;
            _mailService = mailService;
        }

        public IActionResult Login()
        {
            ViewBag.Action = "login";
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Verify(string email)
        {
            email = email ?? "example@email.com";
            return View("verify", email);
        }

        public IActionResult RequestEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (ModelState.IsValid)
            {
                if (Verify(email, password))
                {
                    if (_userDataAccess.GetSpecific<String>(_userDataAccess._userTableName,"confirm", "email", email) == "True")
                    {
                        var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, _userDataAccess.GetUserByEmail(email).Username),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, _userDataAccess.GetSpecific<string>(_userDataAccess._userTableName,"role","email",email))
                    };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        return SendConfirmationEmail(_userDataAccess.GetUserByEmail(email));
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password");
                }
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ResetPass(User model)
        {

            Debug.WriteLine("Email Supplied after getting model " + model.Email);
            Debug.WriteLine("ResetPassToken:" + model.ConfirmationToken);
            if (model.ConfirmationToken != null)
            {
                Debug.WriteLine("Email Supplied "+model.Email);
                ModelState.Clear();
                return View(new ResetPasswordModel { email=model.Email,token=model.ConfirmationToken});
            }

            ViewBag.Message = "Invalid or missing reset password token.";
            return RedirectToAction("RequestEmail", "Auth");
        }


        public IActionResult ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ResetPass", model);
            }

            if (!IsValidResetPasswordToken(model.email, model.token))
            {
                ModelState.AddModelError(" ", "Token Invalid");
                return View("ResetPass", model);
            }


            _userDataAccess.UpdateUserPassword(model.email, model.Password);
            return RedirectToAction("Login", "Auth");
        }


        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (ModelState.IsValid)
            {
                if (_userDataAccess.UserExists("username", model.Username) || _userDataAccess.UserExists("email", model.Email))
                {
                    ModelState.AddModelError("", "Username or email already exists.");
                    return View();
                }

                _userDataAccess.CreateUser(model);
                return SendConfirmationEmail(model);
            }
            return View();
        }

        public IActionResult ConfirmEmail(string token, string resetpass = "False")
        {
            Debug.WriteLine(token + " " + resetpass);
            if (!string.IsNullOrEmpty(token))
            {
                User user = _userDataAccess.GetUserByConfirmationToken(token);
                Debug.WriteLine("User After getting:" + user.Email);
                if (user != null)
                {
                    if (resetpass == "False")
                    {
                        _userDataAccess.ConfirmUserEmail(user);
                        ViewBag.Message = "Email confirmed successfully!";
                        return RedirectToAction("Login", "Auth");
                    }
                    else
                    {

                        Debug.WriteLine("Token Supplied before getting model " + user.ConfirmationToken);
                        return RedirectToAction("ResetPass", "Auth",  user );
                    }
                }
            }
            ViewBag.Message = "Confirmation failed.";
            return RedirectToAction("RequestEmail", "Auth");
        }

        private bool IsValidResetPasswordToken(string email, string resetPassToken)
        {
            return _userDataAccess.UserExists("email", email, "token", resetPassToken);
        }

        private IActionResult SendConfirmationEmail(User user)
        {
            user.ConfirmationToken = Guid.NewGuid().ToString();
            _userDataAccess.UpdateUserToken(user);

            string confirmationLink = Url.Action("ConfirmEmail", "Auth", new { token = user.ConfirmationToken }, Request.Scheme);
            string message = $"Please confirm your email by clicking this link: {confirmationLink}";
            _ = _mailService.SendMail(user.Email, "Email Verification", message);
            return RedirectToAction("Verify", "Auth", new { email = user.Email });
        }

        public IActionResult SendVerificationEmail(RequestEmailModel user)
        {
            if (!ModelState.IsValid)
            {
                Debug.WriteLine("Invalid Model");
                return RedirectToAction("RequestEmail");
            }
            User retrievedUser = _userDataAccess.GetUserByEmail(user.Email);
            if (retrievedUser == null)
            {
                ModelState.AddModelError(" ","No User Found.");
                return RedirectToAction("RequestEmail");
            }

            if (retrievedUser.IsConfirmed == "True")
            {
                return SendResetEmail(_userDataAccess.GetUserByEmail(user.Email));
            }
            Debug.WriteLine("Sent Confirmation");
            return SendConfirmationEmail(_userDataAccess.GetUserByEmail(user.Email));
        }

        public IActionResult SendResetEmail(User user)
        {
            user.ConfirmationToken = Guid.NewGuid().ToString();
            _userDataAccess.UpdateSpecific(_userDataAccess._userTableName, "email", user.Email, "token", user.ConfirmationToken);

            string resetLink = Url.Action(
                "ConfirmEmail", "Auth", new { token = user.ConfirmationToken, resetpass = "True" }, Request.Scheme);

            string message = $"To reset your password, click on the following link: {resetLink}";
            _ = _mailService.SendMail(user.Email, "Password Reset", message);

            return RedirectToAction("Verify","Auth", new { email = user.Email });
        }

        private bool Verify(string email, string password)
        {
            string query = "SELECT password FROM users WHERE email=@Email AND confirm = 'True'";

            using (SqlConnection connection = new SqlConnection(_userDataAccess._connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            string storedHashedPassword = reader.GetString(0);
                            return Crypto.VerifyHashedPassword(storedHashedPassword, password);
                        }
                    }
                }
            }
            return false;
        }

    }
}