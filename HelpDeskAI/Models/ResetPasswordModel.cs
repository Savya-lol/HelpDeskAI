namespace HelpDeskAI.Models
{
    public class ResetPasswordModel
    {
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
            public string email { get; set; }
            public string token { get; set; }
    }
}
