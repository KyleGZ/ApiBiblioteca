using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ApiBiblioteca.Services
{
    public class PasswordService : IPasswordResetService
    {
        private readonly DbContextBiblioteca _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<IPasswordResetService> _logger;
        private readonly IConfiguration _configuration;


        public PasswordService(DbContextBiblioteca dbContextBiblioteca, IEmailService emailService, ILogger<IPasswordResetService> logger, IConfiguration configuration)
        {
            _context = dbContextBiblioteca;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<ApiResponse> GenerateAndSendResetTokenAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Iniciando proceso de reset para: {email}");

                // Buscar usuario por email
                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (user == null)
                {
                    _logger.LogWarning($"Email no registrado: {email}");
                    return new ApiResponse
                    {
                        Success = true,
                        Message = "Si el email existe en nuestro sistema, recibirás un enlace para restablecer tu contraseña."
                    };
                }

                // Verificar si el usuario está activo
                if (user.Estado?.ToLower() != "activo")
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Tu cuenta no está activa. Contacta al administrador."
                    };
                }

                // Generar token seguro
                var token = GenerateSecureToken();
                var expiration = DateTime.UtcNow.AddHours(1);

                // Guardar token usando EF normal (debería funcionar ahora)
                var resetToken = new PasswordResetToken
                {
                    IdUsuario = user.IdUsuario,
                    Token = token,
                    Expires = expiration,
                    Used = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();


                _logger.LogInformation($"Token guardado exitosamente para usuario ID: {user.IdUsuario}");

                // Construir enlace de reset

                var webAppBaseUrl = _configuration["WebAppSettings:BaseUrl"].TrimEnd('/');
                var resetPasswordPath = _configuration["WebAppSettings:ResetPasswordPath"].TrimStart('/');


                var resetLink = $"{webAppBaseUrl}/{resetPasswordPath}?token={Uri.EscapeDataString(token)}";

                // Preparar y enviar email
                var fechaSolicitud = DateTime.Now;
                var emailBody = $@"
                <p>Hola {user.Nombre},</p>
                <p>Se ha recibido una solicitud de recuperación de contraseña para su cuenta.</p>
                <p><a href=""{resetLink}"" style=""background: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Restablecer Contraseña</a></p>
                <p><strong>Enlace directo:</strong> {resetLink}</p>
                <p><em>Este enlace expirará en 1 hora.</em></p>";

                await _emailService.SendAsync(email, "Recuperación de Contraseña - Biblioteca Esparza", emailBody);

                return new ApiResponse
                {
                    Success = true,
                    Message = "Si el email existe en nuestro sistema, recibirás un enlace para restablecer tu contraseña.",
                    Data = new { tokenExpires = expiration }
                };
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Error de BD generando token para: {email}");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Error al procesar la solicitud en la base de datos."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generando token de reset para: {email}");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor."
                };
            }
        }

        public async Task<ApiResponse> ValidateResetTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return new ApiResponse { Success = false, Message = "Token no proporcionado." };

            try
            {
                // Buscar token válido (no usado y no expirado)
                var resetToken = await _context.PasswordResetTokens
                    .Include(t => t.Usuario)
                    .FirstOrDefaultAsync(t =>
                        t.Token == token &&
                        !t.Used &&
                        t.Expires > DateTime.UtcNow);

                if (resetToken == null)
                {
                    return new ApiResponse { Success = false, Message = "El token es inválido o ha expirado." };
                }

                // Verificar que el usuario siga activo
                if (resetToken.Usuario.Estado?.ToLower() != "activo")
                {
                    return new ApiResponse { Success = false, Message = "El usuario asociado a este token no está activo." };
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "Token válido.",
                    Data = new
                    {
                        email = resetToken.Usuario.Email,
                        expires = resetToken.Expires
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validando token: {token}");
                return new ApiResponse { Success = false, Message = "Error validando el token." };
            }
        }

        public async Task<ApiResponse> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                // Validar token
                var validationResult = await ValidateResetTokenAsync(token);
                if (!validationResult.Success)
                {
                    return validationResult; // Retorna el mismo error de validación
                }

                // Obtener el token con usuario
                var resetToken = await _context.PasswordResetTokens
                    .Include(t => t.Usuario)
                    .FirstOrDefaultAsync(t =>
                        t.Token == token &&
                        !t.Used &&
                        t.Expires > DateTime.UtcNow);

                if (resetToken == null)
                {
                    _logger.LogWarning($"Token no encontrado durante reset: {token}");
                    return new ApiResponse { Success = false, Message = "Token no válido." };
                }

                // Verificar que la nueva contraseña sea diferente a la anterior
                if (VerifyPassword(resetToken.Usuario.Password, newPassword))
                {
                    return new ApiResponse { Success = false, Message = "La nueva contraseña no puede ser igual a la anterior." };
                }

                // Marcar token como usado
                resetToken.Used = true;

                // Actualizar contraseña del usuario
                var oldPasswordHash = resetToken.Usuario.Password; // Para logging
                resetToken.Usuario.Password = HashPassword(newPassword);

                // Guardar cambios
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Contraseña restablecida exitosamente para usuario: {resetToken.Usuario.Email}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "Contraseña restablecida exitosamente. Ya puedes iniciar sesión con tu nueva contraseña.",
                    Data = new { email = resetToken.Usuario.Email }
                };
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Error de BD restableciendo contraseña con token: {token}");
                return new ApiResponse { Success = false, Message = "Error al guardar la nueva contraseña." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restableciendo contraseña con token: {token}");
                return new ApiResponse { Success = false, Message = "Error interno al restablecer la contraseña." };
            }
        }

        public async Task<ApiResponse> CleanExpiredTokensAsync()
        {
            try
            {
                // Limpiar tokens expirados o usados (más de 24 horas)
                var expiredTokens = await _context.PasswordResetTokens
                    .Where(t => t.Used || t.Expires < DateTime.UtcNow.AddHours(-24))
                    .ToListAsync();

                int removedCount = 0;
                if (expiredTokens.Any())
                {
                    removedCount = expiredTokens.Count;
                    _context.PasswordResetTokens.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Limpieza de tokens: {removedCount} tokens removidos");
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = $"Limpieza completada. Tokens removidos: {removedCount}",
                    Data = new { tokensRemoved = removedCount }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando tokens expirados");
                return new ApiResponse { Success = false, Message = "Error durante la limpieza de tokens." };
            }
        }

        private string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var base64 = Convert.ToBase64String(bytes);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }


        private string HashPassword(string password)
        {
            // Usa el mismo método de hashing que en tu login actual
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string hashedPassword, string plainPassword)
        {
            // Verificar si la contraseña nueva es igual a la anterior
            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            }
            catch
            {
                return false;
            }
        }


    }

    public interface IPasswordResetService
    {
        Task<ApiResponse> GenerateAndSendResetTokenAsync(string email);
        Task<ApiResponse> ValidateResetTokenAsync(string token);
        Task<ApiResponse> ResetPasswordAsync(string token, string newPassword);
        Task<ApiResponse> CleanExpiredTokensAsync();
    }

}
