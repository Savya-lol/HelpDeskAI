using System.Diagnostics;
using System.Net.Mail;
using System.Net;

namespace HelpDeskAI.Services
{
    public class MailService
    {
        string smtpServer;
        int smtpPort;
        string userName;
        string password;

        public MailService(string smtpServer, int smtpPort, string userName, string password)
        {
            this.smtpServer = smtpServer;
            this.smtpPort = smtpPort;
            this.userName = userName;
            this.password = password;
        }

        public async Task SendMail(string email, string subject, string msg)
        {
            try { 
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
    }
}
