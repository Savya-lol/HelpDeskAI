using System.ComponentModel.DataAnnotations;

namespace HelpDeskAI.Models.Auth
{
    public class Login
    {
        [Required]
        [EmailAddress]
        [MaxLength(50, ErrorMessage = "Email too long")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required for logging in")]
        [MaxLength(50, ErrorMessage = "Password too long")]
        [MinLength(8, ErrorMessage = "Password should be atleast 8 characters long")]
        public string Password { get; set; }
    }
}
