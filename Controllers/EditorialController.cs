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
    public class EditorialController : Controller
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;

        public EditorialController(DbContextBiblioteca dbContext, IAutorizacionService autorizacionService)
        {
            _autorizacionService = autorizacionService;
            _context = dbContext;
        }


        [HttpGet("Lista-Editoriales")]

        public async Task<ActionResult<List<GeneroDto>>> ListaEditoriales(string? nombre)
        {
            try
            {
                var editorialesEcontrados = string.IsNullOrWhiteSpace(nombre)
                   ? await _context.Editorials.ToListAsync()

                   : await _context.Editorials
                          .Where(e => e.Nombre.StartsWith(nombre))
                          .ToListAsync();

                var generosDto = editorialesEcontrados.Select(a => new EditorialDto
                {
                    IdEditorial = a.IdEditorial,
                    Nombre = a.Nombre
                }).ToList();

                return Ok(generosDto);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = "Error interno del servidor al obtener editoriales" });
            }
        }

        // GET /Editorial/Busqueda-Editorial?termino=plan&pagina=1&resultadoPorPagina=20
        [HttpGet("Busqueda-Editorial")]
        public async Task<ActionResult<ApiResponse>> BusquedaEditorial(string termino, int pagina = 1, int resultadoPorPagina = 20)
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
                if (pagina < 1 || resultadoPorPagina < 1)
                {
                    api.Success = false;
                    api.Message = "La página y resultados por página deben ser mayores a 0";
                    return BadRequest(api);
                }

                termino = termino.Trim();

                IQueryable<Editorial> query = _context.Editorials
                    .Where(e => e.Nombre.Contains(termino));

                var totalResultados = await query.CountAsync();

                var editoriales = await query
                    .OrderBy(e => e.IdEditorial)
                    .Select(e => new EditorialDto
                    {
                        IdEditorial = e.IdEditorial,
                        Nombre = e.Nombre
                    })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                api.Success = true;
                api.Message = "Búsqueda realizada correctamente";
                api.Data = new PaginacionResponse<EditorialDto>
                {
                    Success = true,
                    Data = editoriales,
                    Pagination = new PaginationInfo
                    {
                        PaginaActual = pagina,
                        ResultadosPorPagina = resultadoPorPagina,
                        TotalResultados = totalResultados,
                        TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadoPorPagina)
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

        // GET /Editorial/ListarViewEditorial?pagina=1&resultadoPorPagina=20
        [HttpGet("ListarViewEditorial")]
        public async Task<ActionResult<ApiResponse>> ListarViewEditorial(int pagina = 1, int resultadoPorPagina = 20)
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

                IQueryable<Editorial> query = _context.Editorials;

                var totalResultados = await query.CountAsync();

                var editoriales = await query
                    .OrderBy(e => e.IdEditorial)
                    .Select(e => new EditorialDto
                    {
                        IdEditorial = e.IdEditorial,
                        Nombre = e.Nombre
                    })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                api.Success = true;
                api.Message = "Lista de editoriales obtenida exitosamente";
                api.Data = new PaginacionResponse<EditorialDto>
                {
                    Success = true,
                    Data = editoriales,
                    Pagination = new PaginationInfo
                    {
                        PaginaActual = pagina,
                        ResultadosPorPagina = resultadoPorPagina,
                        TotalResultados = totalResultados,
                        TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadoPorPagina)
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

        // POST /Editorial/Registro
        [Authorize(Policy = "StaffOnly")]
        [HttpPost("Registro")]
        public async Task<ActionResult<ApiResponse>> Registro([FromBody] EditorialDto registro)
        {
            var api = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    api.Success = false;
                    api.Message = "Datos de registro inválidos";
                    api.Data = ModelState.Values.SelectMany(v => v.Errors);
                    return BadRequest(api);
                }

                if (string.IsNullOrWhiteSpace(registro?.Nombre))
                {
                    api.Success = false;
                    api.Message = "El nombre es requerido";
                    return BadRequest(api);
                }

                var nombre = registro.Nombre.Trim();

                var existente = await _context.Editorials
                    .FirstOrDefaultAsync(e => e.Nombre.ToLower() == nombre.ToLower());

                if (existente != null)
                {
                    api.Success = false;
                    api.Message = "Ya existe una editorial con ese nombre";
                    return Conflict(api);
                }

                var nueva = new Editorial { Nombre = nombre };

                _context.Editorials.Add(nueva);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Editorial registrada exitosamente";
                api.Data = new { idEditorial = nueva.IdEditorial };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        // PUT /Editorial/Editar
        [Authorize(Policy = "StaffOnly")]
        [HttpPut("Editar")]
        public async Task<ActionResult<ApiResponse>> EditarEditorial([FromBody] EditorialDto editarDto)
        {
            var api = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    api.Success = false;
                    api.Message = "Datos de edición inválidos";
                    api.Data = ModelState.Values.SelectMany(v => v.Errors);
                    return BadRequest(api);
                }

                if (editarDto.IdEditorial <= 0)
                {
                    api.Success = false;
                    api.Message = "El ID de la editorial es requerido";
                    return BadRequest(api);
                }

                var editorial = await _context.Editorials
                    .FirstOrDefaultAsync(e => e.IdEditorial == editarDto.IdEditorial);

                if (editorial == null)
                {
                    api.Success = false;
                    api.Message = "Editorial no encontrada";
                    return NotFound(api);
                }

                if (!string.IsNullOrWhiteSpace(editarDto.Nombre))
                {
                    var nuevoNombre = editarDto.Nombre.Trim();

                    var duplicado = await _context.Editorials
                        .AnyAsync(e => e.IdEditorial != editarDto.IdEditorial &&
                                       e.Nombre.ToLower() == nuevoNombre.ToLower());

                    if (duplicado)
                    {
                        api.Success = false;
                        api.Message = "Ya existe otra editorial con ese nombre";
                        return Conflict(api);
                    }

                    editorial.Nombre = nuevoNombre;
                }

                _context.Editorials.Update(editorial);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Editorial actualizada exitosamente";
                api.Data = new { idEditorial = editorial.IdEditorial };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        // DELETE /Editorial/Eliminar?id=5
        [Authorize(Policy = "StaffOnly")]
        [HttpDelete("Eliminar")]
        public async Task<ActionResult<ApiResponse>> EliminarEditorial(int id)
        {
            var api = new ApiResponse();
            try
            {
                var editorial = await _context.Editorials
                    .Include(e => e.Libros)
                    .FirstOrDefaultAsync(e => e.IdEditorial == id);

                if (editorial == null)
                {
                    api.Success = false;
                    api.Message = "Editorial no encontrada";
                    return NotFound(api);
                }

                if (editorial.Libros.Any())
                {
                    api.Success = false;
                    api.Message = "No se puede eliminar: la editorial tiene libros asociados";
                    return Conflict(api);
                }

                _context.Editorials.Remove(editorial);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Editorial eliminada exitosamente";
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }


    }
}
