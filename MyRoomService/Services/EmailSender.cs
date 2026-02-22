using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
namespace MyRoomService.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("SmtpSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp["FromName"], smtp["FromEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp["Host"], int.Parse(smtp["Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
