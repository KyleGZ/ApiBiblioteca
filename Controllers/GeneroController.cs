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
    public class GeneroController : Controller
    {

        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;

        public GeneroController(DbContextBiblioteca dbContext, IAutorizacionService autorizacionService)
        {
            _autorizacionService = autorizacionService;
            _context = dbContext;
        }

        [HttpGet("Lista-Generos")]
        public async Task<ActionResult<List<GeneroDto>>> ListaGeneros(string? nombre)
        {
            try
            {
                var generosEcontrados = string.IsNullOrWhiteSpace(nombre) 
                   ? await _context.Generos.ToListAsync() 

                   : await _context.Generos
                          .Where(g => g.Nombre.StartsWith(nombre))
                          .ToListAsync();

                var generosDto = generosEcontrados.Select(a => new GeneroDto
                {
                    IdGenero = a.IdGenero,
                    Nombre = a.Nombre
                }).ToList();

                return Ok(generosDto);
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = "Error interno del servidor al obtener generos" });
            }
        }

        // ========= NUEVOS (ApiResponse no genérico) =========
        [HttpGet("ListarViewGenero")]
        public async Task<ActionResult<ApiResponse>> ListarViewGenero(int pagina = 1, int resultadoPorPagina = 20)
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

                var query = _context.Generos.AsQueryable();
                var total = await query.CountAsync();

                var data = await query
                    .OrderBy(g => g.IdGenero)
                    .Select(g => new GeneroDto { IdGenero = g.IdGenero, Nombre = g.Nombre })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                api.Success = true;
                api.Message = "Lista de géneros obtenida exitosamente";
                api.Data = new PaginacionResponse<GeneroDto>
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

        [HttpGet("Busqueda-Genero")]
        public async Task<ActionResult<ApiResponse>> BusquedaGenero(string termino, int pagina = 1, int resultadoPorPagina = 20)
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

                var query = _context.Generos.Where(g => g.Nombre.Contains(termino));
                var total = await query.CountAsync();

                var data = await query
                    .OrderBy(g => g.IdGenero)
                    .Select(g => new GeneroDto { IdGenero = g.IdGenero, Nombre = g.Nombre })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                api.Success = true;
                api.Message = "Búsqueda realizada correctamente";
                api.Data = new PaginacionResponse<GeneroDto>
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

        [Authorize(Policy = ("StaffOnly"))]
        [HttpPost("Registro")]
        public async Task<ActionResult<ApiResponse>> Registro([FromBody] GeneroDto registro)
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

                var existe = await _context.Generos.AnyAsync(g => g.Nombre.ToLower() == nombre.ToLower());
                if (existe)
                {
                    api.Success = false;
                    api.Message = "Ya existe un género con ese nombre";
                    return Conflict(api);
                }

                var nuevo = new Genero { Nombre = nombre };
                _context.Generos.Add(nuevo);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Género registrado exitosamente";
                api.Data = new { idGenero = nuevo.IdGenero };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = ("StaffOnly"))]
        [HttpPut("Editar")]
        public async Task<ActionResult<ApiResponse>> Editar([FromBody] GeneroDto editar)
        {
            var api = new ApiResponse();
            try
            {
                if (editar == null || editar.IdGenero <= 0)
                {
                    api.Success = false;
                    api.Message = "El ID del género es requerido";
                    return BadRequest(api);
                }

                var genero = await _context.Generos.FirstOrDefaultAsync(g => g.IdGenero == editar.IdGenero);
                if (genero == null)
                {
                    api.Success = false;
                    api.Message = "Género no encontrado";
                    return NotFound(api);
                }

                if (!string.IsNullOrWhiteSpace(editar.Nombre))
                {
                    var nuevoNombre = editar.Nombre.Trim();
                    var duplicado = await _context.Generos
                        .AnyAsync(g => g.IdGenero != editar.IdGenero && g.Nombre.ToLower() == nuevoNombre.ToLower());
                    if (duplicado)
                    {
                        api.Success = false;
                        api.Message = "Ya existe otro género con ese nombre";
                        return Conflict(api);
                    }

                    genero.Nombre = nuevoNombre;
                }

                _context.Generos.Update(genero);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Género actualizado exitosamente";
                api.Data = new { idGenero = genero.IdGenero };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = ("StaffOnly"))]
        [HttpDelete("Eliminar")]
        public async Task<ActionResult<ApiResponse>> Eliminar(int id)
        {
            var api = new ApiResponse();
            try
            {
                var genero = await _context.Generos
                    .Include(g => g.IdLibros)
                    .FirstOrDefaultAsync(g => g.IdGenero == id);

                if (genero == null)
                {
                    api.Success = false;
                    api.Message = "Género no encontrado";
                    return NotFound(api);
                }

                if (genero.IdLibros.Any())
                {
                    api.Success = false;
                    api.Message = "No se puede eliminar: el género tiene libros asociados";
                    return Conflict(api);
                }

                _context.Generos.Remove(genero);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Género eliminado exitosamente";
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
