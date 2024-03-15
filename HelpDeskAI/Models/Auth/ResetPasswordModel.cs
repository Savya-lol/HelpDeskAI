using System.ComponentModel.DataAnnotations;

namespace HelpDeskAI.Models.Auth
{
    public class ResetPasswordModel
    {
        [Required]
        public string Password { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }
        public string email { get; set; }
        [Required]
        public string token { get; set; }
    }
}
