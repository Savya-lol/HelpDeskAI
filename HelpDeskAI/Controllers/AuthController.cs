using HelpDeskAI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace HelpDeskAI.Controllers
{
    public class AuthController : Controller
    {
        private readonly SqlConnection con = new SqlConnection();
        private readonly SqlCommand com = new SqlCommand();
        private SqlDataReader dr;

        private void ConnectionString()
        {
            con.ConnectionString = "Server=localhost;Database=helpdeskai;Trusted_Connection=True;";
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
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, "User")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password or unverified account.");
                }
            }

            return View();
        }

        public IActionResult ResetPass(ResetPasswordModel model)
        {

            Debug.WriteLine(model.token);
            if (model.token!=null)
            {
                return View(model);
            }

            ViewBag.Message = "Invalid or missing reset password token.";
            return RedirectToAction("RequestEmail", "Auth");
        }


        public IActionResult ResetPassword(ResetPasswordModel model)
        {
            string password = model.Password;
            string confirmPassword = model.ConfirmPassword;

            if (password.Length >= 8 && (password == confirmPassword) && password != null)
            {

                if (IsValidResetPasswordToken(model.email, model.token))
                {

                    UpdateSpecific("email", model.email, "password", model.Password);

                    TempData.Remove("ResetPassToken");

                    return RedirectToAction("Login", "Auth");
                }

                ViewBag.Message = "Invalid or unauthorized reset password request.";
                return View("ResetPass");
            }

            ModelState.AddModelError("", "Invalid Password, Password must be longer than 8 characters and must match with confirm password");
            return View("ResetPass");
        }

        private bool IsValidResetPasswordToken(string email, string resetPassToken)
        {
            ConnectToDB();
            com.CommandText = "SELECT * FROM users WHERE email=@Email AND token=@Token";
            com.Parameters.AddWithValue("@Email", email);
            com.Parameters.AddWithValue("@Token", resetPassToken);

            dr = com.ExecuteReader();
            bool result = dr.Read();
            dr.Close();
            con.Close();
            return result;
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private bool Verify(string email, string password)
        {
            ConnectToDB();
            com.CommandText = "SELECT * FROM users WHERE email='" + email + "' AND password='" + password + "' AND confirm='True'";
            dr = com.ExecuteReader();
            bool result = dr.Read();
            dr.Close();
            con.Close();
            return result;
        }

        private void SendConfirmationEmail(User user)
        {
            user.ConfirmationToken = Guid.NewGuid().ToString();
            UpdateSpecific("email", user.Email, "token", user.ConfirmationToken);

            string confirmationLink = Url.Action(
                "ConfirmEmail", "Auth", new { token = user.ConfirmationToken }, Request.Scheme);

            string message = $"Please confirm your email by clicking this link: {confirmationLink}";
            SendMail(user.Email, "Email Verification", message);
        }

        public IActionResult SendResetEmail(User user)
        {
            user.ConfirmationToken = Guid.NewGuid().ToString();
            UpdateSpecific("email", user.Email, "token", user.ConfirmationToken);

            // Set reset parameter to true in the link for reset password
            string resetLink = Url.Action(
                "ConfirmEmail", "Auth", new { token = user.ConfirmationToken, resetpass = "True" }, Request.Scheme);

            string message = $"To reset your password, click on the following link: {resetLink}";
            SendMail(user.Email, "Password Reset", message);

            return RedirectToAction("Verify", new {email=user.Email});
        }


        private void SendMail(string email, string subject, string msg)
        {
            string SmtpServer = "smtp-relay.brevo.com";
            int SmtpPort = 587;
            string UserName = "tropedotuber@gmail.com";
            string Password = "Xj9WYRac4pNsQkGM";

            var message = new MailMessage(UserName, email, subject, msg);
            message.IsBodyHtml = true;

            var client = new SmtpClient(SmtpServer);
            client.Credentials = new NetworkCredential(UserName, Password);
            client.Port = SmtpPort;
            client.EnableSsl = true;

            client.Send(message);
        }

        [HttpPost]
        public async Task<IActionResult> Register(User Model)
        {
            if (ModelState.IsValid)
            {
                ConnectToDB();
                if (IsUserExists(Model.Username, Model.Email))
                {
                    ModelState.AddModelError("", "Username or email already exists.");
                    return View();
                }

                CreateUser(Model);

                return RedirectToAction("Verify", "auth", new { email = Model.Email });
            }

            return View();
        }

        private bool IsUserExists(string username, string email)
        {
            com.CommandText = "SELECT COUNT(*) FROM users WHERE username=@Username OR email=@Email";
            com.Parameters.AddWithValue("@Username", username);
            com.Parameters.AddWithValue("@Email", email);

            int count = (int)com.ExecuteScalar();

            return count > 0;
        }

        private void CreateUser(User model)
        {
            if (con.State == System.Data.ConnectionState.Closed)
            {
                ConnectToDB();
            }

            com.CommandText = "INSERT INTO users (first_name, last_name, username, email, password,confirm,token) " +
                              "VALUES (@fname, @lname, @uname, @mail, @pass,@conf_status,@conf_token)";
            com.Parameters.AddWithValue("@fname", model.FirstName);
            com.Parameters.AddWithValue("@lname", model.LastName);
            com.Parameters.AddWithValue("@uname", model.Username);
            com.Parameters.AddWithValue("@mail", model.Email);
            com.Parameters.AddWithValue("@pass", model.Password);
            com.Parameters.AddWithValue("@conf_status", model.IsConfirmed.ToString());
            com.Parameters.AddWithValue("@conf_token", model.ConfirmationToken.ToString());

            com.ExecuteNonQuery();
            con.Close();
            Console.WriteLine("User created successfully.");
            SendConfirmationEmail(model);
        }

        private void UpdateUser(User model)
        {
            if (con.State == System.Data.ConnectionState.Closed)
            {
                ConnectToDB();
            }

            com.CommandText = "UPDATE users SET " +
                              "first_name=@fname, " +
                              "last_name=@lname, " +
                              "email=@mail, " +
                              "password=@pass, " +
                              "confirm=@conf_status " +
                              "WHERE username=@uname";
            com.Parameters.AddWithValue("@fname", model.FirstName);
            com.Parameters.AddWithValue("@lname", model.LastName);
            com.Parameters.AddWithValue("@uname", model.Username);
            com.Parameters.AddWithValue("@mail", model.Email);
            com.Parameters.AddWithValue("@pass", model.Password);
            com.Parameters.AddWithValue("@conf_status", model.IsConfirmed.ToString());
            com.ExecuteNonQuery();
            con.Close();
        }

        public IActionResult ConfirmEmail(string token, string resetpass= "False")
        {
            Debug.WriteLine(token+ " " + resetpass);
            if (!string.IsNullOrEmpty(token))
            {
                User user = GetUserByConfirmationToken(token);
                if (resetpass == "False")
                {
                    if (user != null)
                    {
                        user.IsConfirmed = true;
                        Console.WriteLine("User Found");
                        user.ConfirmationToken = null;
                        UpdateUser(user);
                        ViewBag.Message = "Email confirmed successfully!";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    if (user != null)
                    {
                        TempData["UserEmail"] = user.Email;
                        TempData["ResetPassToken"] = user.ConfirmationToken;
                        var resetPasswordModel = new ResetPasswordModel
                        {
                            email = user.Email,
                            token = user.ConfirmationToken
                        };
                        return RedirectToAction("ResetPass", "Auth", resetPasswordModel);
                    }
                }
            }

            ViewBag.Message = "Confirmation failed.";
            return RedirectToAction("RequestEmail", "Auth");
        }

        private User GetUserByConfirmationToken(string token)
        {
            ConnectToDB();

            com.CommandText = "SELECT * FROM users WHERE token=@conf_token";
            com.Parameters.AddWithValue("@conf_token", token);

            using (var reader = com.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    return new User
                    {
                        FirstName = (string)reader["first_name"],
                        LastName = (string)reader["last_name"],
                        Username = (string)reader["username"],
                        Email = (string)reader["email"],
                        Password = (string)reader["password"],
                        ConfirmationToken = (string)reader["token"]
                    };
                }
                return null;
            }
        }

        private void ConnectToDB()
        {
            if (con.State != System.Data.ConnectionState.Open)
            {
                ConnectionString();
                con.Open();
                com.Connection = con;
            }
            else
            {
                Console.WriteLine("Connection is already open.");
            }

            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
            AND TABLE_NAME = 'users';
        ";

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        reader.Close();

                        string createTable = @"
    CREATE TABLE users (
        id INT PRIMARY KEY IDENTITY(1,1),
        first_name VARCHAR(50) NOT NULL,
        last_name VARCHAR(50) NOT NULL,
        username VARCHAR(25) NOT NULL UNIQUE,
        email VARCHAR(50) NOT NULL UNIQUE,
        password VARCHAR(255) NOT NULL,
        confirm VARCHAR(5),
        role VARCHAR(50) DEFAULT 'User' NOT NULL,
        token VARCHAR(255)
    );";

                        using (var createCmd = con.CreateCommand())
                        {
                            createCmd.CommandText = createTable;
                            createCmd.ExecuteNonQuery();
                            Console.WriteLine("Table 'users' created successfully.");
                        }
                    }
                }
            }
        }

        void UpdateSpecific(string condition, string identifier, string query, object value)
        {
            ConnectToDB();
            com.CommandText = "UPDATE users SET " +
                        query + " = @value " +
                        "WHERE " + condition + " = @identifier";
            com.Parameters.AddWithValue("@value", value);
            com.Parameters.AddWithValue("@identifier", identifier);
            com.ExecuteNonQuery();
        }
    }
}
