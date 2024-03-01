using HelpDeskAI.Models;
using HelpDeskAI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Web.Helpers;

namespace HelpDeskAI.Controllers
{
    public class AuthController : Controller
    {

        private readonly UserDataAccess _userDataAccess;

        public AuthController(UserDataAccess userDataAccess)
        {
            _userDataAccess = userDataAccess;
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
        public async Task<IActionResult> Login(string email, string password)
        {
            if (ModelState.IsValid)
            {
                if (Verify(email, password))
                {
                    if (_userDataAccess.GetSpecific<String>("confirm", "email", email) == "True")
                    {
                        var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, "User") // Assuming a 'User' role
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
                    ModelState.AddModelError("", "Invalid email or password or unverified account.");
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
                        return RedirectToAction("Index", "Home");
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
            SendMail(user.Email, "Email Verification", message);
            return RedirectToAction("Verify", "Auth", new { email = user.Email });
        }

        public IActionResult SendVerificationEmail(User user)
        {
            if(_userDataAccess.GetUserByEmail(user.Email).IsConfirmed=="True")
            {
                return SendResetEmail(user);
            }
            else
            {
               return SendConfirmationEmail(user);
            }
        }

        public IActionResult SendResetEmail(User user)
        {
            user.ConfirmationToken = Guid.NewGuid().ToString();
            _userDataAccess.UpdateSpecific("email", user.Email, "token", user.ConfirmationToken);

            string resetLink = Url.Action(
                "ConfirmEmail", "Auth", new { token = user.ConfirmationToken, resetpass = "True" }, Request.Scheme);

            string message = $"To reset your password, click on the following link: {resetLink}";
            SendMail(user.Email, "Password Reset", message);

            return RedirectToAction("Verify","Auth", new { email = user.Email });
        }

        private async Task SendMail(string email, string subject, string msg)
        {
            try
            {
                string smtpServer = "smtp-relay.brevo.com";
                int smtpPort = 587;
                string userName = "tropedotuber@gmail.com";
                string password = "Xj9WYRac4pNsQkGM";

                var message = new MailMessage(userName, email, subject, msg);
                message.IsBodyHtml = true;

                var client = new SmtpClient(smtpServer);
                client.Credentials = new NetworkCredential(userName, password);
                client.Port = smtpPort;
                client.EnableSsl = true;

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending email: {ex.Message}");
            }
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