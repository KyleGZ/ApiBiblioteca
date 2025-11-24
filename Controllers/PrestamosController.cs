using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace ApiBiblioteca.Controllers
{
    // DTO para actualizar solo la fecha de devolución prevista
    public class ActualizarFechaVencimientoDto
    {
        public DateTime FechaVencimiento { get; set; }
    }
    [Authorize(Policy = "StaffOnly")] 

    [ApiController]
    [Route("api/[controller]")]
    public class PrestamosController : ControllerBase
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;
        private readonly IEmailService _emailService;
        private readonly AutorizacionService _idUsuario;
        private readonly IEstadisticasService _estadisticasService;

        public PrestamosController(DbContextBiblioteca context, IAutorizacionService autorizacionService, IEmailService emailService, IEstadisticasService estadisticasService)
        {
            _context = context;
            _autorizacionService = autorizacionService;
            _emailService = emailService;
            _estadisticasService = estadisticasService;
        }

        //Ultimo prestamo emitido
        [HttpGet("ultimo")]
        public async Task<ActionResult<object>> GetUltimoPrestamo()
        {
            try
            {
                var ultimoPrestamo = await _context.Prestamos
                    .Include(p => p.IdLibroNavigation)
                    .Include(p => p.IdUsuarioNavigation)
                    .Where(p => p.Estado == "Activo")
                    .OrderByDescending(p => p.FechaPrestamo)
                    .Select(p => new
                    {
                        p.IdPrestamo,
                        IdLibro = p.IdLibro,
                        IdUsuario = p.IdUsuario,
                        p.FechaPrestamo,
                        FechaVencimiento = p.FechaDevolucionPrevista,
                        LibroInfo = p.IdLibroNavigation != null ?
                            $"{p.IdLibroNavigation.Titulo} - {p.IdLibroNavigation.Isbn}" : "N/A",
                        UsuarioInfo = p.IdUsuarioNavigation != null ?
                            $"{p.IdUsuarioNavigation.Nombre} - {p.IdUsuarioNavigation.Cedula}" : "N/A",
                        p.Estado
                    })
                    .FirstOrDefaultAsync();

                if (ultimoPrestamo == null)
                {
                    return NotFound(new { message = "No hay préstamos registrados" });
                }

                return Ok(ultimoPrestamo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<Prestamo>> CreatePrestamo([FromBody] CrearPrestamoDto prestamoDto)
        {
            try
            {
                int adminAutenticadoId = AutorizacionService.ObtenerUsuarioAutenticadoId();
                if (adminAutenticadoId == 0)
                {
                    return BadRequest(new { message = "No hay usuario autenticado. Por favor, inicie sesión." });
                }

                int usuarioLectorId = prestamoDto.UsuarioId;
                var usuarioLector = await _context.Usuarios.FindAsync(usuarioLectorId);
                if (usuarioLector == null)
                {
                    return BadRequest(new { message = "El usuario lector no existe" });
                }
                if (!string.Equals(usuarioLector.Estado?.Trim(), "Activo", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "El usuario lector no está activo" });
                }

                var libro = await _context.Libros.FindAsync(prestamoDto.LibroId);
                if (libro == null)
                {
                    return BadRequest(new { message = "El libro no existe" });
                }

                var libroPrestado = await _context.Prestamos
                    .AnyAsync(p => p.IdLibro == prestamoDto.LibroId && p.Estado == "Activo");

                if (libroPrestado)
                {
                    return BadRequest(new { message = "El libro no está disponible (ya está prestado)" });
                }

                if (prestamoDto.FechaVencimiento <= prestamoDto.FechaPrestamo)
                {
                    return BadRequest(new { message = "La fecha de vencimiento debe ser posterior a la fecha de préstamo" });
                }

                // ACTUALIZAR ESTADO DEL LIBRO
                libro.Estado = "Prestado";

                var prestamo = new Prestamo
                {
                    IdLibro = prestamoDto.LibroId,
                    IdUsuario = usuarioLectorId,
                    FechaPrestamo = prestamoDto.FechaPrestamo,
                    FechaDevolucionPrevista = prestamoDto.FechaVencimiento,
                    FechaDevolucionReal = null,
                    Renovaciones = 0,
                    Estado = "Activo"
                };

                _context.Prestamos.Add(prestamo);
                await _context.SaveChangesAsync();

                await _context.Entry(prestamo).Reference(p => p.IdUsuarioNavigation).LoadAsync();
                await _context.Entry(prestamo).Reference(p => p.IdLibroNavigation).LoadAsync();

                var response = new
                {
                    Id = prestamo.IdPrestamo,
                    IdLibro = prestamo.IdLibro,
                    IdUsuario = prestamo.IdUsuario,
                    prestamo.FechaPrestamo,
                    FechaVencimiento = prestamo.FechaDevolucionPrevista,
                    LibroInfo = prestamo.IdLibroNavigation != null ?
                        $"{prestamo.IdLibroNavigation.Titulo} - {prestamo.IdLibroNavigation.Isbn}" : $"Libro {prestamo.IdLibro}",
                    UsuarioInfo = prestamo.IdUsuarioNavigation != null ?
                        $"{prestamo.IdUsuarioNavigation.Nombre} - {prestamo.IdUsuarioNavigation.Cedula}" : $"Usuario {prestamo.IdUsuario}",
                    prestamo.Estado,
                    CreadoPorAdmin = adminAutenticadoId
                };

                return CreatedAtAction(nameof(GetUltimoPrestamo), response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el préstamo", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // NUEVO: Actualizar solo la fecha de devolución prevista manteniendo el préstamo activo
        [HttpPut("fecha-vencimiento/{idPrestamo}")]
        public async Task<ActionResult<object>> ActualizarFechaVencimiento(int idPrestamo,[FromBody] ActualizarFechaVencimientoDto dto)
        {
            try
            {
                var prestamo = await _context.Prestamos.FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo);

                if (prestamo == null)
                {
                    return NotFound(new { message = "Préstamo no encontrado" });
                }

                if (!string.Equals(prestamo.Estado?.Trim(), "Activo", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Solo se puede actualizar la fecha de préstamos activos" });
                }

                if (dto == null || dto.FechaVencimiento == default)
                {
                    return BadRequest(new { message = "Fecha de vencimiento inválida" });
                }

                if (dto.FechaVencimiento <= prestamo.FechaDevolucionPrevista)
                {
                    return BadRequest(new { message = "La nueva fecha debe ser posterior a la fecha vigente" });
                }

                if (dto.FechaVencimiento <= prestamo.FechaPrestamo)
                {
                    return BadRequest(new { message = "La nueva fecha debe ser posterior a la fecha de préstamo" });
                }

                var fechaAnterior = prestamo.FechaDevolucionPrevista;
                prestamo.FechaDevolucionPrevista = dto.FechaVencimiento;
                prestamo.Renovaciones = prestamo.Renovaciones + 1;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    prestamo.IdPrestamo,
                    FechaAnterior = fechaAnterior,
                    NuevaFechaVencimiento = prestamo.FechaDevolucionPrevista,
                    prestamo.Renovaciones,
                    prestamo.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la fecha de vencimiento", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Usuarios Activos
        [HttpGet("usuarios/activos")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsuariosActivos()
        {
            try
            {
                var usuarios = await _context.Usuarios
                    .Where(u => u.Estado == "Activo")
                    .Select(u => new
                    {
                        Id = u.IdUsuario,
                        u.Nombre,
                        u.Cedula,
                        u.Email,
                        DisplayText = $"{u.Nombre} - {u.Cedula}"
                    })
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cargar usuarios", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Libros disponibles
        [HttpGet("libros/disponibles")]
        public async Task<ActionResult<IEnumerable<object>>> GetLibrosDisponibles()
        {
            try
            {
                var librosPrestados = await _context.Prestamos
                    .Where(p => p.Estado == "Activo")
                    .Select(p => p.IdLibro)
                    .ToListAsync();

                var libros = await _context.Libros
                    .Where(l => !librosPrestados.Contains(l.IdLibro) && l.Estado.ToLower() == "disponible")
                    .Select(l => new
                    {
                        Id = l.IdLibro,
                        l.Titulo,
                        l.Isbn,
                        l.Descripcion,
                        DisplayText = $"{l.Titulo} - {l.Isbn}"
                    })
                    .ToListAsync();

                return Ok(libros);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cargar libros", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Lista prestamos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPrestamos()
        {
            try
            {
                var prestamos = await _context.Prestamos
                    .Include(p => p.IdUsuarioNavigation)
                    .Include(p => p.IdLibroNavigation)
                    .OrderByDescending(p => p.FechaPrestamo)
                    .Select(p => new
                    {
                        Id = p.IdPrestamo,
                        IdLibro = p.IdLibro,
                        IdUsuario = p.IdUsuario,
                        p.FechaPrestamo,
                        FechaVencimiento = p.FechaDevolucionPrevista,
                        FechaDevolucion = p.FechaDevolucionReal,
                        p.Estado,
                        p.Renovaciones,
                        LibroTitulo = p.IdLibroNavigation != null ? p.IdLibroNavigation.Titulo : "N/A",
                        LibroIsbn = p.IdLibroNavigation != null ? p.IdLibroNavigation.Isbn : "N/A",
                        UsuarioNombre = p.IdUsuarioNavigation != null ? p.IdUsuarioNavigation.Nombre : "N/A",
                        UsuarioCedula = p.IdUsuarioNavigation != null ? p.IdUsuarioNavigation.Cedula : "N/A"
                    })
                    .ToListAsync();

                return Ok(prestamos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cargar préstamos", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Devolución de libros
        [HttpPut("devolucion/{idPrestamo}")]
        public async Task<ActionResult<object>> RegistrarDevolucion(int idPrestamo)
        {
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.IdLibroNavigation)
                    .Include(p => p.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo && p.Estado == "Activo");

                if (prestamo == null)
                {
                    return NotFound(new { message = "Préstamo no encontrado o ya devuelto" });
                }

                prestamo.FechaDevolucionReal = DateTime.Now;
                prestamo.Estado = "Devuelto";
                
                
                if (prestamo.IdLibroNavigation != null)
                {
                    prestamo.IdLibroNavigation.Estado = "Disponible"; 
                }

                await _context.SaveChangesAsync();

                var response = new
                {
                    prestamo.IdPrestamo,
                    Mensaje = "Devolución registrada exitosamente",
                    FechaDevolucion = prestamo.FechaDevolucionReal,
                    Libro = prestamo.IdLibroNavigation?.Titulo ?? "N/A",
                    Usuario = prestamo.IdUsuarioNavigation?.Nombre ?? "N/A"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al registrar la devolución", error = ex.Message });
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Muestra los préstamos activos
        [HttpGet("activos")]
        public async Task<ActionResult<IEnumerable<object>>> GetPrestamosActivos()
        {
            try
            {
                var prestamos = await _context.Prestamos
                    .Include(p => p.IdUsuarioNavigation)
                    .Include(p => p.IdLibroNavigation)
                    .Where(p => p.Estado == "Activo")       
                    .OrderByDescending(p => p.FechaPrestamo)
                    .Select(p => new
                    {
                        p.IdPrestamo,
                        IdLibro = p.IdLibro,
                        IdUsuario = p.IdUsuario,
                        p.FechaPrestamo,
                        FechaVencimiento = p.FechaDevolucionPrevista,
                        LibroTitulo = p.IdLibroNavigation != null ? p.IdLibroNavigation.Titulo : "N/A",
                        LibroIsbn = p.IdLibroNavigation != null ? p.IdLibroNavigation.Isbn : "N/A",
                        UsuarioNombre = p.IdUsuarioNavigation != null ? p.IdUsuarioNavigation.Nombre : "N/A",
                        UsuarioCedula = p.IdUsuarioNavigation != null ? p.IdUsuarioNavigation.Cedula : "N/A",
                        p.Estado,
                        DiasRetraso = EF.Functions.DateDiffDay(DateTime.Now, p.FechaDevolucionPrevista) > 0 ?
                            EF.Functions.DateDiffDay(DateTime.Now, p.FechaDevolucionPrevista) : 0
                    })
                    .ToListAsync();

                return Ok(prestamos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al cargar préstamos activos", error = ex.Message });
            }
        }

        ///------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        /// 
        [HttpGet("GetEstadisticasPrestamos")]
        public async Task<ActionResult<EstadisticasPrestamosDTO>> GetEstadisticasPrestamos()
        {
            try
            {
                var estadisticas = await _estadisticasService.ObtenerEstadisticasPrestamosAsync();
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener estadísticas: {ex.Message}");
            }
        }

        [HttpPost("GetEstadisticasPorFiltro")]
        public async Task<ActionResult<EstadisticasPrestamosDTO>> GetEstadisticasPorFiltro([FromBody] FiltroEstadisticasDTO filtro)
        {
            try
            {
                var estadisticas = await _estadisticasService.ObtenerEstadisticasPorRangoAsync(filtro);
                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener estadísticas: {ex.Message}");
            }
        }

        [HttpPost("DescargarReporteExcel")]
        public async Task<IActionResult> DescargarReporteExcel([FromBody] FiltroEstadisticasDTO filtro)
        {
            try
            {
                var excelBytes = await _estadisticasService.GenerarReporteExcelAsync(filtro);

                var fileName = $"ReportePrestamos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar reporte: {ex.Message}");
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Buscar préstamos por usuario, libro o ISBN
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarPrestamos(string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino)) { return BadRequest(new { message = "Debe proporcionar un término de búsqueda" }); }
                termino = termino.Trim().ToLower();

                var resultados = await _context.Prestamos
                    .Include(p => p.IdUsuarioNavigation)
                    .Include(p => p.IdLibroNavigation)
                    .Where(p =>
                        (p.IdUsuarioNavigation != null && p.IdUsuarioNavigation.Nombre.ToLower().Contains(termino)) ||
                        (p.IdLibroNavigation != null && p.IdLibroNavigation.Titulo.ToLower().Contains(termino)) ||
                        (p.IdLibroNavigation != null && p.IdLibroNavigation.Isbn.ToLower().Contains(termino))
                    )
                    .OrderByDescending(p => p.FechaPrestamo)
                    .Select(p => new
                    {
                        p.IdPrestamo,
                        UsuarioNombre = p.IdUsuarioNavigation != null ? p.IdUsuarioNavigation.Nombre : "N/A",
                        LibroTitulo = p.IdLibroNavigation != null ? p.IdLibroNavigation.Titulo : "N/A",
                        LibroIsbn = p.IdLibroNavigation != null ? p.IdLibroNavigation.Isbn : "N/A",
                        p.FechaPrestamo,
                        FechaVencimiento = p.FechaDevolucionPrevista,
                        p.Estado,
                        EstadoLibro = p.IdLibroNavigation != null ? p.IdLibroNavigation.Estado : "N/A"
                    })
                    .ToListAsync();

                return Ok(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al buscar préstamos: {ex.Message}" });
            }
        }
    

    }//fin del public
}//fin del namespace