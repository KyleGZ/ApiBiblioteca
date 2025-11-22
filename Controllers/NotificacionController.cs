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
        public async Task<ActionResult<List<NotificacionView>>> ObtenerNotificaciones([FromQuery] int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return BadRequest("El ID de usuario proporcionado no es válido.");
            }

            try
            {
                var notificaciones = await _context.Notificacions
                    .Where(n => n.IdUsuario == idUsuario)
                    .OrderByDescending(n => n.FechaEnvio)
                    .Select(n => new NotificacionView
                    {
                        IdNotificacion = n.IdNotificacion,
                        IdUsuario = n.IdUsuario,
                        Asunto = n.Asunto,
                        Mensaje = n.Mensaje,
                        FechaEnvio = n.FechaEnvio,
                        Estado = n.Estado
                    })
                    .ToListAsync();

                return Ok(notificaciones);
            }
            catch (Exception ex)
            {
                // Opcional: puedes también devolver StatusCode(500, "mensaje") o registrar el error
                return StatusCode(500, $"Ocurrió un error al obtener las notificaciones: {ex.Message}");
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
                    .Where(n => n.IdUsuario == idUsuario && n.Estado == "No leída")
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

        //*Metodo para eliminar notificaciones antiguas

        [HttpDelete("EliminarTodas")]
        public async Task<ActionResult<ApiResponse>> EliminarTodas(int idUsuario)
        {
            try
            {
                if (idUsuario <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID de usuario no es válido."
                    });
                }

                // Buscar todas las notificaciones del usuario
                var notificaciones = await _context.Notificacions
                    .Where(n => n.IdUsuario == idUsuario)
                    .ToListAsync();

                if (!notificaciones.Any())
                {
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Message = "No hay notificaciones para eliminar."
                    });
                }

                // Eliminar todas
                _context.Notificacions.RemoveRange(notificaciones);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Todas las notificaciones fueron eliminadas correctamente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error interno en el servidor: {ex.Message}"
                });
            }
        }



    }
}