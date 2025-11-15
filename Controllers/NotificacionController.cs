using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificacionController : Controller
    {
        private readonly INotificacionesServices _notificacionesService;
        private readonly DbContextBiblioteca _context;

        public NotificacionController(INotificacionesServices notificaciones, DbContextBiblioteca dbContext)
        {
            _context = dbContext;
            _notificacionesService = notificaciones;
        }

        [HttpGet("ProbarRecordatorio")]
        public async Task<IActionResult> ProbarRecordatorio()
        {
            var resultado = await _notificacionesService.RecordarPrestamosPorVencerAsync();
            return Ok(resultado);
        }

        [HttpGet("ObtenerNotificaciones")]
        public async Task<ActionResult<ApiResponse>> ObtenerNotificaciones([FromQuery] int idUsuario)
        {
            try
            {
                if (idUsuario <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID de usuario proporcionado no es válido.",
                        Data = null
                    });
                }

                var notificaciones = await _context.Notificacions
                    .Where(n => n.IdUsuario == idUsuario)
                    .OrderByDescending(n => n.FechaEnvio)
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Notificaciones obtenidas exitosamente.",
                    Data = notificaciones
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Ocurrió un error al obtener las notificaciones: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpPut("MarcarLeida")]
        public async Task<ActionResult<ApiResponse>> MarcarNotificacionLeida([FromQuery] int idNotificacion)
        {
            try
            {
                if (idNotificacion <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID de la notificación no es válido.",
                        Data = null
                    });
                }

                var notificacion = await _context.Notificacions
                    .FirstOrDefaultAsync(n => n.IdNotificacion == idNotificacion);

                if (notificacion == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "La notificación no existe.",
                        Data = null
                    });
                }

                notificacion.Estado = "Leída";

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Notificación marcada como leída.",
                    Data = notificacion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al marcar la notificación como leída: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpPut("MarcarTodasLeidas")]
        public async Task<ActionResult<ApiResponse>> MarcarTodasComoLeidas([FromQuery] int idUsuario)
        {
            try
            {
                if (idUsuario <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID del usuario no es válido.",
                        Data = null
                    });
                }

                var notificaciones = await _context.Notificacions
                    .Where(n => n.IdUsuario == idUsuario && n.Estado == "No leida")
                    .ToListAsync();

                if (!notificaciones.Any())
                {
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "No hay notificaciones pendientes por leer.",
                        Data = null
                    });
                }

                foreach (var notif in notificaciones)
                {
                    notif.Estado = "Leída";
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Se marcaron {notificaciones.Count} notificaciones como leídas.",
                    Data = notificaciones
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al marcar las notificaciones como leídas: {ex.Message}",
                    Data = null
                });
            }
        }


    }
}