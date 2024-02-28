using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpDeskAI.Models
{
    public class User
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required for registration")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required for registration")]
        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
