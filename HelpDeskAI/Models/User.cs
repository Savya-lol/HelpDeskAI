using System.ComponentModel.DataAnnotations;

namespace HelpDeskAI.Models
{
    public class User
    {
        [Required]
        [MaxLength(50,ErrorMessage ="First Name too long")]
        public string FirstName { get; set; }
        [Required]
        [MaxLength(50, ErrorMessage = "Last Name too long")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required for registration")]
        [MaxLength(25, ErrorMessage = "Username too long")]
        public string Username { get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Email too long")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required for registration")]
        [MaxLength(50, ErrorMessage = "Password too long")]
        [MinLength(8, ErrorMessage ="Password should be atleast 8 characters long")]
        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
