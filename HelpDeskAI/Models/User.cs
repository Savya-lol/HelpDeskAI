using System.ComponentModel.DataAnnotations;

namespace HelpDeskAI.Models
{
    public class User
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        [Required]
        public string Username { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        
        public string ConfirmPassword { get; set; }
    }
}
