using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReservaController : Controller
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;
        private readonly IEmailService _emailService;

        public ReservaController(DbContextBiblioteca context, IAutorizacionService autorizacionService, IEmailService emailService)
        {
            _context = context;
            _autorizacionService = autorizacionService;
            _emailService = emailService;

        }

        // GET: api/Reservas - Lista todas las reservas
        [HttpGet("ListaReservas")]
        public async Task<ActionResult<IEnumerable<ReservaResponseDto>>> GetReservas()
        {
            var reservas = await _context.Reservas
                .Include(r => r.IdLibroNavigation)      // Incluir datos del libro
                .Include(r => r.IdUsuarioNavigation)    // Incluir datos del usuario
                .Select(r => new ReservaResponseDto
                {
                    IdReserva = r.IdReserva,
                    IdUsuario = r.IdUsuario,
                    IdLibro = r.IdLibro,
                    FechaReserva = r.FechaReserva,
                    Prioridad = r.Prioridad,
                    Estado = r.Estado,
                    TituloLibro = r.IdLibroNavigation.Titulo,
                    NombreUsuario = r.IdUsuarioNavigation.Nombre,
                    Isbn = r.IdLibroNavigation.Isbn
                })
                .ToListAsync();

            return Ok(reservas);
        }

        // GET: api/Reservas/5 - Búsqueda por ID de reserva
        [HttpGet("BuscarReservaID")]
        public async Task<ActionResult<ReservaResponseDto>> GetReserva(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.IdLibroNavigation)
                .Include(r => r.IdUsuarioNavigation)
                .Where(r => r.IdReserva == id)
                .Select(r => new ReservaResponseDto
                {
                    IdReserva = r.IdReserva,
                    IdUsuario = r.IdUsuario,
                    IdLibro = r.IdLibro,
                    FechaReserva = r.FechaReserva,
                    Prioridad = r.Prioridad,
                    Estado = r.Estado,
                    TituloLibro = r.IdLibroNavigation.Titulo,
                    NombreUsuario = r.IdUsuarioNavigation.Nombre,
                    Isbn = r.IdLibroNavigation.Isbn
                })
                .FirstOrDefaultAsync();

            if (reserva == null)
            {
                return NotFound(new { message = $"No se encontró la reserva con ID {id}" });
            }

            return Ok(reserva);
        }


        // POST: api/Reservas/CrearReserva
        [HttpPost("CrearReserva")]
        public async Task<ActionResult<ApiResponse>> CrearReserva(ReservaDto reservaDto)
        {
            try
            {
                // 1️⃣ Validar existencia del libro
                var libro = await _context.Libros.FindAsync(reservaDto.IdLibro);
                if (libro == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El libro no existe"
                    });
                }

                // 2️⃣ Validar existencia del usuario
                var usuario = await _context.Usuarios.FindAsync(reservaDto.IdUsuario);
                if (usuario == null)
                {
                    return BadRequest(new ApiResponse { 
                    Success = false,
                    Message = "El usuario no existe"
                    });
                }

                // 3️⃣ Si el libro está disponible, no se crea reserva
                if (libro.Estado == "Disponible")
                {
                    return BadRequest(new ApiResponse {
                        Success= false,
                        Message= "El libro está disponible. Debes acudir a la biblioteca para gestionar el préstamo."
                    });
                }

                // 4️⃣ Verificar si el usuario ya tiene una reserva pendiente o confirmada para este libro
                var reservaExistente = await _context.Reservas
                    .FirstOrDefaultAsync(r =>
                        r.IdUsuario == reservaDto.IdUsuario &&
                        r.IdLibro == reservaDto.IdLibro &&
                        (r.Estado == "Pendiente" || r.Estado == "Confirmada")
                    );

                if (reservaExistente != null)
                {
                    return BadRequest(new ApiResponse
                    {

                        Success = false,
                        Message = "Ya tienes una reserva activa o pendiente para este libro."
                    });
                }

                // 5️⃣ Calcular la prioridad automáticamente
                var maxPrioridad = await _context.Reservas
                    .Where(r => r.IdLibro == reservaDto.IdLibro && r.Estado == "Pendiente")
                    .MaxAsync(r => (int?)r.Prioridad) ?? 0;

                int nuevaPrioridad = maxPrioridad + 1;

                // 6️⃣ Crear la nueva reserva con estado 'Pendiente'
                var reserva = new Reserva
                {
                    IdUsuario = reservaDto.IdUsuario,
                    IdLibro = reservaDto.IdLibro,
                    FechaReserva = DateTime.Now,
                    Prioridad = nuevaPrioridad,
                    Estado = "Pendiente"
                };

                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();

                // 7️⃣ Consultar la reserva creada con navegación
                var reservaCreada = await _context.Reservas
                    .Include(r => r.IdLibroNavigation)
                    .Include(r => r.IdUsuarioNavigation)
                    .Where(r => r.IdReserva == reserva.IdReserva)
                    .Select(r => new ReservaResponseDto
                    {
                        IdReserva = r.IdReserva,
                        IdUsuario = r.IdUsuario,
                        IdLibro = r.IdLibro,
                        FechaReserva = r.FechaReserva,
                        Prioridad = r.Prioridad,
                        Estado = r.Estado,
                        TituloLibro = r.IdLibroNavigation.Titulo,
                        NombreUsuario = r.IdUsuarioNavigation.Nombre,
                        Isbn = r.IdLibroNavigation.Isbn
                    })
                    .FirstOrDefaultAsync();

                // 8️⃣ Retornar respuesta exitosa
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Reserva creada exitosamente.",
                    Data = reservaCreada
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { 
                Success = false,
                Message = "Error interno del servidor",
                Data = ex.InnerException.Message
                });
            }
        }


        /*
         * Metodo opcional
         */

        //// POST: api/Reservas - Crear nueva reserva
        //[HttpPost("CrearReserva")]
        //public async Task<ActionResult<ReservaResponseDto>> CrearReserva(ReservaDto reservaDto)
        //{
        //    try
        //    {
        //        // Validar que el libro existe y está disponible
        //        var libro = await _context.Libros.FindAsync(reservaDto.IdLibro);
        //        if (libro == null)
        //        {
        //            return BadRequest(new { message = "El libro no existe" });
        //        }

        //        if (libro.Estado != "Disponible")
        //        {
        //            return BadRequest(new { message = "El libro no está disponible para reserva" });
        //        }

        //        // Validar que el usuario existe
        //        var usuario = await _context.Usuarios.FindAsync(reservaDto.IdUsuario);
        //        if (usuario == null)
        //        {
        //            return BadRequest(new { message = "El usuario no existe" });
        //        }

        //        // Verificar si el usuario ya tiene una reserva activa para este libro
        //        var reservaExistente = await _context.Reservas
        //            .Where(r => r.IdUsuario == reservaDto.IdUsuario &&
        //                   r.IdLibro == reservaDto.IdLibro &&
        //                   r.Estado == "Activa")
        //            .FirstOrDefaultAsync();

        //        if (reservaExistente != null)
        //        {
        //            return BadRequest(new { message = "Ya tienes una reserva activa para este libro" });
        //        }

        //        // Crear la nueva reserva
        //        var reserva = new Reserva
        //        {
        //            IdUsuario = reservaDto.IdUsuario,
        //            IdLibro = reservaDto.IdLibro,
        //            FechaReserva = DateTime.Now,
        //            Prioridad = reservaDto.Prioridad,
        //            Estado = "Activa",
        //            IdLibroNavigation = libro,
        //            IdUsuarioNavigation = usuario
        //        };

        //        // Cambiar estado del libro a "Reservado"
        //        libro.Estado = "Reservado";
        //        _context.Entry(libro).State = EntityState.Modified;

        //        _context.Reservas.Add(reserva);
        //        await _context.SaveChangesAsync();

        //        // Obtener la reserva creada con los datos de navegación
        //        var reservaCreada = await _context.Reservas
        //            .Include(r => r.IdLibroNavigation)
        //            .Include(r => r.IdUsuarioNavigation)
        //            .Where(r => r.IdReserva == reserva.IdReserva)
        //            .Select(r => new ReservaResponseDto
        //            {
        //                IdReserva = r.IdReserva,
        //                IdUsuario = r.IdUsuario,
        //                IdLibro = r.IdLibro,
        //                FechaReserva = r.FechaReserva,
        //                Prioridad = r.Prioridad,
        //                Estado = r.Estado,
        //                TituloLibro = r.IdLibroNavigation.Titulo,
        //                NombreUsuario = r.IdUsuarioNavigation.Nombre,
        //                Isbn = r.IdLibroNavigation.Isbn
        //            })
        //            .FirstOrDefaultAsync();

        //        return CreatedAtAction(nameof(GetReserva), new { id = reserva.IdReserva }, reservaCreada);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        //    }
        //}

        // DELETE: api/Reservas/5 - Eliminar/Cancelar reserva
        [HttpDelete("EliminarReserva")]
        public async Task<IActionResult> EliminarReserva(int id)
        {
            try
            {
                var reserva = await _context.Reservas
                    .Include(r => r.IdLibroNavigation)
                    .FirstOrDefaultAsync(r => r.IdReserva == id);

                if (reserva == null)
                {
                    return NotFound(new { message = $"No se encontró la reserva con ID {id}" });
                }

                // Liberar el libro (cambiar estado a "Disponible")
                var libro = await _context.Libros.FindAsync(reserva.IdLibro);
                if (libro != null)
                {
                    libro.Estado = "Disponible";
                    _context.Entry(libro).State = EntityState.Modified;
                }

                // Eliminar la reserva de la base de datos
                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Reserva eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar la reserva", error = ex.Message });
            }
        }

        //// DELETE: api/Reservas/5/cancelar - Alternativa: Cancelar reserva (cambiar estado en lugar de eliminar)
        //[HttpDelete("{id}/cancelar")]
        //public async Task<IActionResult> CancelarReserva(int id)
        //{
        //    try
        //    {
        //        var reserva = await _context.Reservas
        //            .Include(r => r.IdLibroNavigation)
        //            .FirstOrDefaultAsync(r => r.IdReserva == id);

        //        if (reserva == null)
        //        {
        //            return NotFound(new { message = $"No se encontró la reserva con ID {id}" });
        //        }

        //        // Liberar el libro
        //        var libro = await _context.Libros.FindAsync(reserva.IdLibro);
        //        if (libro != null)
        //        {
        //            libro.Estado = "Disponible";
        //            _context.Entry(libro).State = EntityState.Modified;
        //        }

        //        // Cambiar estado de la reserva a "Cancelada" en lugar de eliminar
        //        reserva.Estado = "Cancelada";
        //        _context.Entry(reserva).State = EntityState.Modified;

        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Reserva cancelada correctamente" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error al cancelar la reserva", error = ex.Message });
        //    }
        //}

    }//
}//
