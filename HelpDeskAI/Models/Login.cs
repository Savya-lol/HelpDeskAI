using System.ComponentModel.DataAnnotations;

namespace HelpDeskAI.Models
{
    public class Login
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required for logging in")]
        public string Password { get; set; }
    }
}
