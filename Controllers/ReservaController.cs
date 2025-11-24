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

        [HttpGet("ListaReservas")]
        public async Task<ActionResult<IEnumerable<ReservaResponseDto>>> GetReservas(
    [FromQuery] int? userId = null,
    [FromQuery] string? estado = null,
    [FromQuery] bool? esAdministrador = null)
        {
            var query = _context.Reservas
                .Include(r => r.IdLibroNavigation)
                .Include(r => r.IdUsuarioNavigation)
                .AsQueryable();

            //SI NO ES ADMINISTRADOR, filtrar por usuario
            if (!esAdministrador.HasValue || !esAdministrador.Value)
            {
                if (userId.HasValue)
                {
                    query = query.Where(r => r.IdUsuario == userId.Value);
                }
                else
                {
                    // Si no es admin y no hay userId, no mostrar nada
                    return Ok(new List<ReservaResponseDto>());
                }
            }
            //SI ES ADMINISTRADOR, mostrar TODAS las reservas sin filtrar por usuario
            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "Activa")
                {
                    query = query.Where(r => r.Estado == "Activa");
                }
                else if (estado == "NoActiva")
                {
                    query = query.Where(r => r.Estado != "Activa");
                }
            }

            var reservas = await query
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

        // Búsqueda por ID de reserva
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

        [HttpPost("CrearReserva")]
        public async Task<ActionResult<ApiResponse>> CrearReserva(ReservaDto reservaDto)
        {
            try
            {
                // Validar existencia del libro
                var libro = await _context.Libros.FindAsync(reservaDto.IdLibro);
                if (libro == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El libro no existe"
                    });
                }

                // Validar existencia del usuario
                var usuario = await _context.Usuarios.FindAsync(reservaDto.IdUsuario);
                if (usuario == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El usuario no existe"
                    });
                }

                // Verificar que el usuario no tenga más de 3 reservas activas
                var reservasActivasUsuario = await _context.Reservas
                    .Where(r => r.IdUsuario == reservaDto.IdUsuario && r.Estado == "Activa")
                    .CountAsync();

                if (reservasActivasUsuario >= 3)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Has alcanzado el límite máximo de 3 reservas activas. Debes cancelar o completar alguna reserva existente antes de crear una nueva."
                    });
                }

                // Solo se puede reservar si el libro está DISPONIBLE
                if (libro.Estado != "Disponible")
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El libro no está disponible para reserva."
                    });
                }

                // Verificar si el usuario ya tiene una reserva activa para este libro específico
                var reservaExistenteMismoLibro = await _context.Reservas
                    .FirstOrDefaultAsync(r =>
                        r.IdUsuario == reservaDto.IdUsuario &&
                        r.IdLibro == reservaDto.IdLibro &&
                        r.Estado == "Activa"
                    );

                if (reservaExistenteMismoLibro != null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Ya tienes una reserva activa para este libro."
                    });
                }

                //Calcular la prioridad automáticamente
                var maxPrioridad = await _context.Reservas
                    .Where(r => r.IdLibro == reservaDto.IdLibro && r.Estado == "Activa")
                    .MaxAsync(r => (int?)r.Prioridad) ?? 0;

                int nuevaPrioridad = maxPrioridad + 1;

                // Crear la nueva reserva con estado 'Activa'
                var reserva = new Reserva
                {
                    IdUsuario = reservaDto.IdUsuario,
                    IdLibro = reservaDto.IdLibro,
                    FechaReserva = DateTime.Now,
                    Prioridad = nuevaPrioridad,
                    Estado = "Activa"
                };

                // Cambiar estado del libro a "No disponible"
                libro.Estado = "Reservado";
                _context.Entry(libro).State = EntityState.Modified;

                _context.Reservas.Add(reserva);
                await _context.SaveChangesAsync();

                // Consultar la reserva creada con navegación
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

                //  Obtener el nuevo conteo de reservas activas
                var nuevoConteoReservas = reservasActivasUsuario + 1;

                // Retornar respuesta exitosa con información del límite
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Reserva creada exitosamente. Tienes {nuevoConteoReservas} de 3 reservas activas.",
                    Data = new
                    {
                        Reserva = reservaCreada,
                        ReservasActivas = nuevoConteoReservas,
                        LimiteReservas = 3
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor",
                    Data = ex.Message
                });
            }
        }

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

                // cambiar estado a "Cancelada"
                reserva.Estado = "Cancelada";

                // Liberar el libro (cambiar estado a "Disponible")
                var libro = await _context.Libros.FindAsync(reserva.IdLibro);
                if (libro != null)
                {
                    libro.Estado = "Disponible";
                    _context.Entry(libro).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Reserva cancelada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cancelar la reserva", error = ex.Message });
            }
        }

        // Endpoint para obtener conteo de reservas activas por usuario
        [HttpGet("ConteoReservasActivas")]
        public async Task<ActionResult<ApiResponse>> GetConteoReservasActivas([FromQuery] int userId)
        {
            try
            {
                var conteo = await _context.Reservas
                    .Where(r => r.IdUsuario == userId && r.Estado == "Activa")
                    .CountAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Conteo obtenido exitosamente",
                    Data = conteo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al obtener el conteo",
                    Data = 0
                });
            }
        }


        //[Authorize(Policy = "GeneralPolicy")]
        //[HttpGet("Get-reserva")]
        //public async Task<ActionResult<int>> GetReserva(string idReserva)
        //{
        //    try
        //    {
        //        var autor = await _context.Reservas.FirstOrDefaultAsync(x => x.IdReserva == idReserva);
        //        if (autor == null)
        //        {
        //            return NotFound(new { message = "Autor no encontrado" });
        //        }
        //        var idAutor = autor.IdAutor;
        //        return Ok(idAutor);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error al obtener el autor: {ex.Message}");
        //        return StatusCode(500, new { error = "Error interno del servidor" });
        //    }

        //}

    }//
}//
