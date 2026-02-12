namespace househub.Models
{
    // SMTP email kuldes beallitasai az appsettings.json fajlbol.
    public class SmtpEmailSettings
    {
        public const string SectionName = "EmailSettings";

        // Kapcsoljuk be/kikapcsoljuk az email kuldest konfigbol.
        public bool Enabled { get; set; } = false;

        // SMTP szerver host neve (pl. smtp.gmail.com).
        public string Host { get; set; } = string.Empty;

        // SMTP port (pl. 587 STARTTLS-hez, 465 SSL-hez).
        public int Port { get; set; } = 587;

        // SMTP usernev (altalaban email cim).
        public string UserName { get; set; } = string.Empty;

        // SMTP jelszo vagy app password.
        public string Password { get; set; } = string.Empty;

        // TLS/SSL bekapcsolasa.
        public bool EnableSsl { get; set; } = true;

        // Felado email cim.
        public string FromEmail { get; set; } = string.Empty;

        // Felado nev.
        public string FromName { get; set; } = "househub";
    }
}
