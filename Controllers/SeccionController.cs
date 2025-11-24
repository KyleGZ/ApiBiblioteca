using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    
    public class SeccionController : Controller
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;
        public SeccionController(DbContextBiblioteca dbContext, IAutorizacionService autorizacionService)
        {
            _autorizacionService = autorizacionService;
            _context = dbContext;
        }

        [HttpGet("Lista-Secciones")]
        public async Task<ActionResult<List<GeneroDto>>> ListaSecciones(string? nombre)
        {
            try
            {
                var seccionesEcontrados = string.IsNullOrWhiteSpace(nombre)
                   ? await _context.Seccions.ToListAsync()

                   : await _context.Seccions
                          .Where(s => s.Nombre.StartsWith(nombre))
                          .ToListAsync();

                var seccionDto = seccionesEcontrados.Select(s => new SeccionDto
                {
                    IdSeccion = s.IdSeccion,
                    Nombre = s.Nombre
                }).ToList();

                return Ok(seccionDto);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = "Error interno del servidor al obtener secciones" });
            }
        }

        // ========= NUEVOS (ApiResponse no genérico) =========
        [HttpGet("ListarViewSeccion")]
        public async Task<ActionResult<ApiResponse>> ListarViewSeccion(int pagina = 1, int resultadoPorPagina = 20)
        {
            var api = new ApiResponse();
            try
            {
                if (pagina < 1 || resultadoPorPagina < 1)
                {
                    api.Success = false;
                    api.Message = "La página y resultados por página deben ser mayores a 0";
                    return BadRequest(api);
                }

                var query = _context.Seccions.AsQueryable();
                var total = await query.CountAsync();

                var data = await query
                    .OrderBy(s => s.IdSeccion)
                    .Select(s => new SeccionDto { IdSeccion = s.IdSeccion, Nombre = s.Nombre })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                api.Success = true;
                api.Message = "Lista de secciones obtenida exitosamente";
                api.Data = new PaginacionResponse<SeccionDto>
                {
                    Success = true,
                    Data = data,
                    Pagination = new PaginationInfo
                    {
                        PaginaActual = pagina,
                        ResultadosPorPagina = resultadoPorPagina,
                        TotalResultados = total,
                        TotalPaginas = (int)Math.Ceiling(total / (double)resultadoPorPagina)
                    }
                };

                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [HttpGet("Busqueda-Seccion")]
        public async Task<ActionResult<ApiResponse>> BusquedaSeccion(string termino, int pagina = 1, int resultadoPorPagina = 20)
        {
            var api = new ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    api.Success = false;
                    api.Message = "El término de búsqueda no puede estar vacío";
                    return BadRequest(api);
                }

                termino = termino.Trim();

                var query = _context.Seccions
                    .Where(s => s.Nombre.Contains(termino) || s.Ubicacion.Contains(termino));

                var total = await query.CountAsync();

                var data = await query
                    .OrderBy(s => s.IdSeccion)
                    .Select(s => new SeccionDto { IdSeccion = s.IdSeccion, Nombre = s.Nombre })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                api.Success = true;
                api.Message = "Búsqueda realizada correctamente";
                api.Data = new PaginacionResponse<SeccionDto>
                {
                    Success = true,
                    Data = data,
                    Pagination = new PaginationInfo
                    {
                        PaginaActual = pagina,
                        ResultadosPorPagina = resultadoPorPagina,
                        TotalResultados = total,
                        TotalPaginas = (int)Math.Ceiling(total / (double)resultadoPorPagina)
                    }
                };

                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpPost("Registro")]
        public async Task<ActionResult<ApiResponse>> Registro([FromBody] SeccionDto registro)
        {
            var api = new ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(registro?.Nombre))
                {
                    api.Success = false;
                    api.Message = "El nombre es requerido";
                    return BadRequest(api);
                }

                var nombre = registro.Nombre.Trim();

                var existe = await _context.Seccions.AnyAsync(s => s.Nombre.ToLower() == nombre.ToLower());
                if (existe)
                {
                    api.Success = false;
                    api.Message = "Ya existe una sección con ese nombre";
                    return Conflict(api);
                }

                // La entidad requiere Ubicacion; si no la manejas en el front, se deja un valor por defecto:
                var nueva = new Seccion { Nombre = nombre, Ubicacion = "Sin ubicación" };
                _context.Seccions.Add(nueva);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Sección registrada exitosamente";
                api.Data = new { idSeccion = nueva.IdSeccion };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpPut("Editar")]
        public async Task<ActionResult<ApiResponse>> Editar([FromBody] SeccionDto editar)
        {
            var api = new ApiResponse();
            try
            {
                if (editar == null || editar.IdSeccion <= 0)
                {
                    api.Success = false;
                    api.Message = "El ID de la sección es requerido";
                    return BadRequest(api);
                }

                var seccion = await _context.Seccions.FirstOrDefaultAsync(s => s.IdSeccion == editar.IdSeccion);
                if (seccion == null)
                {
                    api.Success = false;
                    api.Message = "Sección no encontrada";
                    return NotFound(api);
                }

                if (!string.IsNullOrWhiteSpace(editar.Nombre))
                {
                    var nuevoNombre = editar.Nombre.Trim();
                    var duplicado = await _context.Seccions
                        .AnyAsync(s => s.IdSeccion != editar.IdSeccion && s.Nombre.ToLower() == nuevoNombre.ToLower());
                    if (duplicado)
                    {
                        api.Success = false;
                        api.Message = "Ya existe otra sección con ese nombre";
                        return Conflict(api);
                    }

                    seccion.Nombre = nuevoNombre;
                }

                _context.Seccions.Update(seccion);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Sección actualizada exitosamente";
                api.Data = new { idSeccion = seccion.IdSeccion };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpDelete("Eliminar")]
        public async Task<ActionResult<ApiResponse>> Eliminar(int id)
        {
            var api = new ApiResponse();
            try
            {
                var seccion = await _context.Seccions
                    .Include(s => s.Libros)
                    .FirstOrDefaultAsync(s => s.IdSeccion == id);

                if (seccion == null)
                {
                    api.Success = false;
                    api.Message = "Sección no encontrada";
                    return NotFound(api);
                }

                if (seccion.Libros.Any())
                {
                    api.Success = false;
                    api.Message = "No se puede eliminar: la sección tiene libros asociados";
                    return Conflict(api);
                }

                _context.Seccions.Remove(seccion);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Sección eliminada exitosamente";
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("Get-seccion")]
        public async Task<ActionResult<int>> GetSeccion(string nombre)
        {
            try
            {
                var seccion = await _context.Seccions.FirstOrDefaultAsync(x => x.Nombre == nombre);
                if (seccion == null)
                {
                    return NotFound(new { message = "Seccion no encontrada" });
                }
                var idSeccion = seccion.IdSeccion;
                return Ok(idSeccion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la seccion: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }

        }
    }
}
