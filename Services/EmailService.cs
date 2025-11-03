using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using Microsoft.Extensions.Options;
using ApiBiblioteca.Models.Dtos;
namespace ApiBiblioteca.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
        {
            _emailSettings = options.Value;
            _logger = logger;
        }

        /*
         * Envía un correo electrónico simple sin archivos adjuntos.
         */
        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var message = CreateMessage(toEmail, subject, htmlBody);
            await SendMessageAsync(message, cancellationToken);
        }

        /*
         * Envía un correo electrónico con un archivo adjunto.
         */
        public async Task SendWithAttachmentAsync(string toEmail, string subject, string htmlBody, byte[] attachmentBytes, string attachmentFilename, string mimeType = "application/pdf", CancellationToken cancellationToken = default)
        {
            var message = CreateMessage(toEmail, subject, htmlBody);

            var builder = new BodyBuilder();
            builder.HtmlBody = htmlBody;
            builder.Attachments.Add(attachmentFilename, attachmentBytes, ContentType.Parse(mimeType));

            message.Body = builder.ToMessageBody();

            await SendMessageAsync(message, cancellationToken);
        }

        // Crea el mensaje de correo electrónico.
        private MimeMessage CreateMessage(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }

        // Envía el mensaje de correo electrónico utilizando SMTP.
        private async Task SendMessageAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient();

            try
            {
                // Connect
                var secureSocketOptions = _emailSettings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions, cancellationToken);

                // Authenticate only if username provided
                if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
                {
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
                }   

                // Send
                await client.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo a {To}. Subject: {Subject}", message.To, message.Subject);
                throw; // opcional: envuelve en custom exception si quieres manejarlo distinto
            }
            finally
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

    }
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendWithAttachmentAsync(string toEmail, string subject, string htmlBody, byte[] attachmentBytes, string attachmentFilename, string mimeType = "application/pdf", CancellationToken cancellationToken = default);
    }

}
