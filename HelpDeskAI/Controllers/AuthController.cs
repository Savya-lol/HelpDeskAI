using HelpDeskAI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Reflection;
using System.Security.Claims;

namespace HelpDeskAI.Controllers
{
    public class AuthController : Controller
    {
        SqlConnection con = new SqlConnection();
        SqlCommand com = new SqlCommand();
        SqlDataReader dr;

        void ConnectionString()
        {
            con.ConnectionString = "Server=localhost;Database=helpdeskai;Trusted_Connection=True;";
        }
        public IActionResult Login()
        {
            ViewBag.Action = "login";
            return View(); ;
        }
        public IActionResult Register()
        {
            ViewBag.Action = "register";
            return View();
        }

        public IActionResult Verify(string email)
        {
            email = email == null? "example@email.com":email;
            return View("verify",email);
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
                    ModelState.AddModelError("", "Invalid username or password.");
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

        bool Verify(string email, string password)
        {
            ConnectToDB();
            com.CommandText = "select * from users where email='" + email + "' and password='" + password + "'";
            dr = com.ExecuteReader();
            bool result = dr.Read();
            dr.Close();
            con.Close();
            return result;
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
                Login(Model.Email, Model.Password);

                return RedirectToAction("Index", "Home");
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
            com.CommandText = "INSERT INTO users (first_name, last_name, username, email, password,confirm) " +
                              "VALUES (@fname, @lname, @uname, @mail, @pass,@isconfirmed)";
            com.Parameters.AddWithValue("@fname", model.FirstName);
            com.Parameters.AddWithValue("@lname", model.LastName);
            com.Parameters.AddWithValue("@uname", model.Username);
            com.Parameters.AddWithValue("@mail", model.Email);
            com.Parameters.AddWithValue("@pass", model.Password);
            com.Parameters.AddWithValue("@isconfirmed", model.IsConfirmed.ToString());

            com.ExecuteNonQuery();
            Console.WriteLine("User created successfully.");
            con.Close();
        }

        void ConnectToDB()
        {
            ConnectionString();
            con.Open();
            com.Connection = con;

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
                    role varchar(50),);";

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

    }

}
