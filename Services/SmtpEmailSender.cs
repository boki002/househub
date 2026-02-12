using System.Net;
using System.Net.Mail;
using househub.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace househub.Services
{
    // Identity altal hasznalt email kuldo szolgaltatas SMTP alapon.
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpEmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<SmtpEmailSettings> settings,
            ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Ha nincs bekapcsolva, csak logolunk es kilepunk.
            if (!_settings.Enabled)
            {
                _logger.LogWarning("Email kuldes kihagyva, mert EmailSettings.Enabled=false.");
                return;
            }

            // Alap konfig ellenorzes, hogy ne homalyos SMTP hiba legyen futaskor.
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                throw new InvalidOperationException("Hianyos EmailSettings konfiguracio (Host/FromEmail).");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(email));

            using var smtp = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                smtp.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
            }

            await smtp.SendMailAsync(message);
        }
    }
}
