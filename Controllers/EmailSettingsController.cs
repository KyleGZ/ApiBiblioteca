using ApiBiblioteca.Services;
using ApiBiblioteca.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ApiBiblioteca.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class EmailSettingsController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailSettingsController> _logger;

        public EmailSettingsController(IEmailService emailService, ILogger<EmailSettingsController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la configuración de correo electrónico actual.

        [HttpGet("GetSettings")]
        public async Task<ActionResult<EmailSettings>> GetSettings()
        {
            var settings = await _emailService.GetSettingsAsync();
            return Ok(settings);
        }

        /// <summary>
        /// Actualiza la configuración de correo electrónico.

        [HttpPut("UpdateSttings")]
        public async Task<IActionResult> UpdateSttings([FromBody] EmailSettings settings)
        {
            if (settings == null)
                return BadRequest("Datos inválidos.");

            try
            {
                await _emailService.UpdateSettingsAsync(settings);
                return Ok(new { message = "Configuración de correo actualizada correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando configuración de correo.");
                return StatusCode(500, "Error al actualizar la configuración de correo.");
            }
        }

        /// <summary>
        /// Prueba la conexión SMTP usando la configuración actual.
        [HttpGet("TestConnection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var success = await _emailService.TestConnectionAsync();
                if (success)
                    return Ok(new { message = "Conexión SMTP exitosa." });

                return StatusCode(500, new { message = "No se pudo conectar al servidor SMTP." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error probando conexión SMTP.");
                return StatusCode(500, "Error probando la conexión SMTP.");
            }
        }


    }
}
