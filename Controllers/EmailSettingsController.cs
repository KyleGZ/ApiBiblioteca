using ApiBiblioteca.Models.Dtos;
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
        [HttpPut("UpdateSettings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateEmailSettings settings)
        {
            if (settings == null)
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Datos inválidos."
                });

            var result = await _emailService.UpdateSettingsAsync(settings);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        /// <summary>
        /// Prueba la conexión SMTP usando la configuración actual.
        [HttpGet("TestConnection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var result = await _emailService.TestConnectionAsync();

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado probando conexión SMTP.");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error inesperado probando la conexión SMTP."
                });
            }
        }

    }
}
