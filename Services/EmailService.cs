using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using MimeKit;

namespace ApiBiblioteca.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IDataProtector _protector;
        private readonly string _filePath;
        private EmailSettings _emailSettings;

        public EmailService(ILogger<EmailService> logger, IDataProtectionProvider dataProtectionProvider, IWebHostEnvironment env)
        {
            _logger = logger;
            _protector = dataProtectionProvider.CreateProtector("EmailSettingsProtector");

            // ✅ 1. Usa IWebHostEnvironment para que funcione tanto en desarrollo como en producción
            _filePath = Path.Combine(env.ContentRootPath, "App_Data", "emailsettings.json");

            // Cargar configuración inicial
            _emailSettings = LoadSettingsAsync().GetAwaiter().GetResult();
        }

        private async Task<EmailSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Archivo de configuración de correo no encontrado en {Path}. Se usará configuración vacía.", _filePath);
                return new EmailSettings();
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<EmailSettings>(json) ?? new EmailSettings();

            if (!string.IsNullOrWhiteSpace(settings.Password))
            {
                try { settings.Password = _protector.Unprotect(settings.Password); }
                catch { settings.Password = string.Empty; }
            }

            return settings;
        }

        /*
         * Envía un correo electrónico simple sin archivos adjuntos.
         */
        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            ValidateSettings();
            var message = CreateMessage(toEmail, subject, htmlBody);
            await SendMessageAsync(message, cancellationToken);
        }

        /*
         * Envía un correo electrónico con un archivo adjunto.
         */
        public async Task SendWithAttachmentAsync(string toEmail, string subject, string htmlBody,
            byte[] attachmentBytes, string attachmentFilename, string mimeType = "application/pdf",
            CancellationToken cancellationToken = default)
        {
            ValidateSettings();

            var message = CreateMessage(toEmail, subject, htmlBody);
            var builder = new BodyBuilder { HtmlBody = htmlBody };
            builder.Attachments.Add(attachmentFilename, attachmentBytes, ContentType.Parse(mimeType));
            message.Body = builder.ToMessageBody();

            await SendMessageAsync(message, cancellationToken);
        }

        private MimeMessage CreateMessage(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();
            return message;
        }

        private async Task SendMessageAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient();

            try
            {
                _logger.LogInformation("Conectando a servidor SMTP {Host}:{Port}", _emailSettings.SmtpHost, _emailSettings.SmtpPort);

                var secureSocketOptions = _emailSettings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
                {
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                _logger.LogInformation("Correo enviado exitosamente a {To}.", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo a {To}. Asunto: {Subject}", message.To, message.Subject);
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        public async Task<EmailSettings> GetSettingsAsync()
        {
            return await LoadSettingsAsync();
        }


        //public async Task UpdateSettingsAsync(EmailSettings newSettings)
        //{
        //    if (string.IsNullOrWhiteSpace(newSettings.SmtpHost))
        //        throw new ArgumentException("El servidor SMTP no puede estar vacío.");
        //    if (string.IsNullOrWhiteSpace(newSettings.FromEmail))
        //        throw new ArgumentException("El correo de origen no puede estar vacío.");

        //    // ✅ 2. Asegurar carpeta App_Data
        //    Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        //    // Crear respaldo
        //    if (File.Exists(_filePath))
        //    {
        //        var backupPath = _filePath.Replace(".json", $"_backup_{DateTime.Now:yyyyMMddHHmmss}.json");
        //        File.Copy(_filePath, backupPath, true);
        //    }

        //    // Encriptar contraseña antes de guardar
        //    if (!string.IsNullOrWhiteSpace(newSettings.Password))
        //    {
        //        newSettings.Password = _protector.Protect(newSettings.Password);
        //    }

        //    // Guardar archivo
        //    var json = System.Text.Json.JsonSerializer.Serialize(newSettings, new System.Text.Json.JsonSerializerOptions
        //    {
        //        WriteIndented = true
        //    });

        //    await File.WriteAllTextAsync(_filePath, json);
        //    _logger.LogInformation("Configuración de correo actualizada correctamente en {Path}.", _filePath);

        //    // ✅ 3. Recargar configuración en memoria
        //    _emailSettings = await LoadSettingsAsync();
        //}

        public async Task<ApiResponse> UpdateSettingsAsync(UpdateEmailSettings newSettings)
        {
            // 🧱 1. Validar campos obligatorios (excepto la contraseña)
            if (string.IsNullOrWhiteSpace(newSettings.SmtpHost))
                return new ApiResponse { Success = false, Message = "El servidor SMTP no puede estar vacío." };

            if (newSettings.SmtpPort == null || newSettings.SmtpPort <= 0)
                return new ApiResponse { Success = false, Message = "El puerto SMTP debe ser un número válido." };

            if (newSettings.UseStartTls == null)
                return new ApiResponse { Success = false, Message = "Debe especificar si se utiliza STARTTLS." };

            if (string.IsNullOrWhiteSpace(newSettings.FromEmail))
                return new ApiResponse { Success = false, Message = "El correo de origen no puede estar vacío." };

            if (string.IsNullOrWhiteSpace(newSettings.FromName))
                return new ApiResponse { Success = false, Message = "El nombre del remitente no puede estar vacío." };

            if (string.IsNullOrWhiteSpace(newSettings.Username))
                return new ApiResponse { Success = false, Message = "El nombre de usuario no puede estar vacío." };

            try
            {
                // 🗂️ 2. Asegurar carpeta App_Data
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                // 📂 3. Cargar configuración actual
                EmailSettings currentSettings;
                if (File.Exists(_filePath))
                {
                    var currentJson = await File.ReadAllTextAsync(_filePath);
                    currentSettings = System.Text.Json.JsonSerializer.Deserialize<EmailSettings>(currentJson) ?? new EmailSettings();
                }
                else
                {
                    currentSettings = new EmailSettings();
                }

                // 💾 4. Crear respaldo
                if (File.Exists(_filePath))
                {
                    var backupPath = _filePath.Replace(".json", $"_backup_{DateTime.Now:yyyyMMddHHmmss}.json");
                    File.Copy(_filePath, backupPath, true);
                }

                // 🔄 5. Asignar nuevos valores (campos obligatorios)
                currentSettings.SmtpHost = newSettings.SmtpHost;
                currentSettings.SmtpPort = newSettings.SmtpPort;
                currentSettings.UseStartTls = newSettings.UseStartTls;
                currentSettings.FromEmail = newSettings.FromEmail;
                currentSettings.FromName = newSettings.FromName;
                currentSettings.Username = newSettings.Username;

                // 🔐 6. Manejar la contraseña (solo actualizar si se envía)
                if (!string.IsNullOrWhiteSpace(newSettings.Password))
                {
                    currentSettings.Password = _protector.Protect(newSettings.Password);
                }
                // Si viene null o vacía → conservar la anterior (no se hace nada)

                // 🧾 7. Guardar archivo actualizado
                var json = System.Text.Json.JsonSerializer.Serialize(currentSettings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_filePath, json);
                _logger.LogInformation("Configuración de correo actualizada correctamente en {Path}.", _filePath);

                // ♻️ 8. Recargar configuración en memoria
                _emailSettings = await LoadSettingsAsync();

                return new ApiResponse { Success = true, Message = "Configuración de correo actualizada correctamente." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando configuración de correo.");
                return new ApiResponse { Success = false, Message = $"Error al actualizar la configuración de correo: {ex.Message}" };
            }
        }




        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var client = new SmtpClient();
                var secureSocketOptions = _emailSettings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

                await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureSocketOptions);

                if (!string.IsNullOrWhiteSpace(_emailSettings.Username))
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                await client.DisconnectAsync(true);
                _logger.LogInformation("Conexión SMTP exitosa.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error probando conexión SMTP.");
                return false;
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpHost) ||
                string.IsNullOrWhiteSpace(_emailSettings.FromEmail))
            {
                throw new InvalidOperationException("Configuración de correo no válida. Verifique emailsettings.json.");
            }
        }
    }

    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
        Task SendWithAttachmentAsync(string toEmail, string subject, string htmlBody, byte[] attachmentBytes, string attachmentFilename, string mimeType = "application/pdf", CancellationToken cancellationToken = default);
        Task<EmailSettings> GetSettingsAsync();
        Task <ApiResponse>UpdateSettingsAsync(UpdateEmailSettings newSettings);
        Task<bool> TestConnectionAsync();
    }
}
