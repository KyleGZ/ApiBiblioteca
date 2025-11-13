using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Añadir esto para requerir autenticación
    public class PrestamosController : ControllerBase
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;
        private readonly IEmailService _emailService;
        private readonly AutorizacionService _idUsuario;

        public PrestamosController(DbContextBiblioteca context, IAutorizacionService autorizacionService, IEmailService emailService)
        {
            _context = context;
            _autorizacionService = autorizacionService;
            _emailService = emailService;
        }


 //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
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

        ////Guardar prestamos
        //[HttpPost]
        //public async Task<ActionResult<Prestamo>> CreatePrestamo([FromBody] CrearPrestamoDto prestamoDto)
        //{
        //    try
        //    {
        //        // Validar que el usuario existe
        //        var usuario = await _context.Usuarios.FindAsync(prestamoDto.UsuarioId);
        //        if (usuario == null)
        //        {
        //            return BadRequest(new { message = "El usuario no existe" });
        //        }

        //        // Validar que el libro existe
        //        var libro = await _context.Libros.FindAsync(prestamoDto.LibroId);
        //        if (libro == null)
        //        {
        //            return BadRequest(new { message = "El libro no existe" });
        //        }

        //        // Validar disponibilidad - verificar si el libro ya está prestado
        //        var libroPrestado = await _context.Prestamos
        //            .AnyAsync(p => p.IdLibro == prestamoDto.LibroId && p.Estado == "Activo");

        //        if (libroPrestado)
        //        {
        //            return BadRequest(new { message = "El libro no está disponible (ya está prestado)" });
        //        }

        //        // Validar fechas
        //        if (prestamoDto.FechaVencimiento <= prestamoDto.FechaPrestamo)
        //        {
        //            return BadRequest(new { message = "La fecha de vencimiento debe ser posterior a la fecha de préstamo" });
        //        }

        //        // Crear el préstamo
        //        var prestamo = new Prestamo
        //        {
        //            IdLibro = prestamoDto.LibroId,
        //            IdUsuario = prestamoDto.UsuarioId,
        //            FechaPrestamo = prestamoDto.FechaPrestamo,
        //            FechaDevolucionPrevista = prestamoDto.FechaVencimiento,
        //            FechaDevolucionReal = null,
        //            Renovaciones = 0,
        //            Estado = "Activo"
        //        };

        //        // Guardar en la base de datos
        //        _context.Prestamos.Add(prestamo);
        //        await _context.SaveChangesAsync();

        //        // Cargar datos relacionados
        //        await _context.Entry(prestamo)
        //            .Reference(p => p.IdUsuarioNavigation)
        //            .LoadAsync();

        //        await _context.Entry(prestamo)
        //            .Reference(p => p.IdLibroNavigation)
        //            .LoadAsync();

        //        // Retornar respuesta
        //        var response = new
        //        {
        //            Id = prestamo.IdPrestamo,
        //            IdLibro = prestamo.IdLibro,
        //            IdUsuario = prestamo.IdUsuario,
        //            prestamo.FechaPrestamo,
        //            FechaVencimiento = prestamo.FechaDevolucionPrevista,
        //            LibroInfo = prestamo.IdLibroNavigation != null ?
        //                $"{prestamo.IdLibroNavigation.Titulo} - {prestamo.IdLibroNavigation.Isbn}" : $"Libro {prestamo.IdLibro}",
        //            UsuarioInfo = prestamo.IdUsuarioNavigation != null ?
        //                $"{prestamo.IdUsuarioNavigation.Nombre} - {prestamo.IdUsuarioNavigation.Cedula}" : $"Usuario {prestamo.IdUsuario}",
        //            prestamo.Estado
        //        };

        //        return CreatedAtAction(nameof(GetUltimoPrestamo), response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error al crear el préstamo", error = ex.Message });
        //    }
        //}
        [HttpPost]
        public async Task<ActionResult<Prestamo>> CreatePrestamo([FromBody] CrearPrestamoDto prestamoDto)
        {
            try
            {
                // Simplemente usar el UsuarioId que viene en el DTO
                // (ya que no tienes autenticación funcionando correctamente)
                int usuarioAutenticadoId = AutorizacionService.ObtenerUsuarioAutenticadoId();

                // Validar que el usuario existe y está activo
                var usuario = await _context.Usuarios.FindAsync(usuarioAutenticadoId);
                if (usuario == null)
                {
                    return BadRequest(new { message = "El usuario no existe" });
                }
                if (!string.Equals(usuario.Estado?.Trim(), "Activo", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "El usuario no está activo" });
                }

                // Validar que el libro existe
                var libro = await _context.Libros.FindAsync(prestamoDto.LibroId);
                if (libro == null)
                {
                    return BadRequest(new { message = "El libro no existe" });
                }

                // Validar disponibilidad - verificar si el libro ya está prestado
                var libroPrestado = await _context.Prestamos
                    .AnyAsync(p => p.IdLibro == prestamoDto.LibroId && p.Estado == "Activo");

                if (libroPrestado)
                {
                    return BadRequest(new { message = "El libro no está disponible (ya está prestado)" });
                }

                // Validar fechas
                if (prestamoDto.FechaVencimiento <= prestamoDto.FechaPrestamo)
                {
                    return BadRequest(new { message = "La fecha de vencimiento debe ser posterior a la fecha de préstamo" });
                }

                // Crear el préstamo
                var prestamo = new Prestamo
                {
                    IdLibro = prestamoDto.LibroId,
                    IdUsuario = usuarioAutenticadoId,
                    FechaPrestamo = prestamoDto.FechaPrestamo,
                    FechaDevolucionPrevista = prestamoDto.FechaVencimiento,
                    FechaDevolucionReal = null,
                    Renovaciones = 0,
                    Estado = "Activo"
                };

                // Guardar en la base de datos
                _context.Prestamos.Add(prestamo);
                await _context.SaveChangesAsync();

                // Cargar datos relacionados
                await _context.Entry(prestamo)
                    .Reference(p => p.IdUsuarioNavigation)
                    .LoadAsync();

                await _context.Entry(prestamo)
                    .Reference(p => p.IdLibroNavigation)
                    .LoadAsync();

                // Retornar respuesta
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
                    prestamo.Estado
                };

                return CreatedAtAction(nameof(GetUltimoPrestamo), response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el préstamo", error = ex.Message });
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
                    .Where(u => u.Estado == "Activo") // Usar Estado en lugar de Activo
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
                // Obtener IDs de libros que están prestados actualmente
                var librosPrestados = await _context.Prestamos
                    .Where(p => p.Estado == "Activo")
                    .Select(p => p.IdLibro)
                    .ToListAsync();

                // Libros que NO están en la lista de prestados y tienen estado activo
                var libros = await _context.Libros
                    .Where(l => !librosPrestados.Contains(l.IdLibro) && l.Estado == "Activo")
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
                // Buscar el préstamo
                var prestamo = await _context.Prestamos
                    .Include(p => p.IdLibroNavigation)
                    .Include(p => p.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(p => p.IdPrestamo == idPrestamo && p.Estado == "Activo");

                if (prestamo == null)
                {
                    return NotFound(new { message = "Préstamo no encontrado o ya devuelto" });
                }

                // Actualizar el préstamo
                prestamo.FechaDevolucionReal = DateTime.Now;
                prestamo.Estado = "Devuelto";

                // Marcar el libro como disponible (aunque no tenemos propiedad disponible, el libro estará disponible automáticamente al no tener préstamos activos)

                await _context.SaveChangesAsync();

                // Retornar respuesta
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


    }//fin del public
}//fin del namespace