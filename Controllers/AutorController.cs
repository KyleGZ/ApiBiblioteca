using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace ApiBiblioteca.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AutorController : Controller
    {
        
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;

        public AutorController(DbContextBiblioteca dbContext, IAutorizacionService autorizacion)
        {
            _autorizacionService = autorizacion;
            _context = dbContext;
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpGet("Lista-Autores")]
        public async Task<ActionResult<List<AutorDto>>> BuscarAutorNombre(string? nombre)
        {
            try
            {
                // Si no se aporta un nombre, traemos todos
                var autoresEncontrados = string.IsNullOrWhiteSpace(nombre)
                    ? await _context.Autors.ToListAsync()
                    : await _context.Autors
                        .Where(a => a.Nombre.StartsWith(nombre))
                        .ToListAsync();

                var dtos = autoresEncontrados.Select(a => new AutorDto
                {
                    IdAutor = a.IdAutor,
                    Nombre = a.Nombre
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                // Opcional: registrar el error
                return StatusCode(500, new { error = "Error interno del servidor al obtener autores" });
            }
        }
        [Authorize(Policy = "StaffOnly")]
        // GET /Autor/Busqueda-Autor
        [HttpGet("Busqueda-Autor")]
        public async Task<ActionResult<PaginacionResponse<AutorDto>>> BusquedaAutor(string termino, int pagina = 1, int resultadoPorPagina = 20)
        {
            var response = new PaginacionResponse<AutorDto>();

            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return BadRequest(new PaginacionResponse<AutorDto>
                    {
                        Success = false,
                        Message = "El término de búsqueda no puede estar vacío"
                    });
                }

                if (pagina < 1 || resultadoPorPagina < 1)
                {
                    return BadRequest(new PaginacionResponse<AutorDto>
                    {
                        Success = false,
                        Message = "La página y resultados por página deben ser mayores a 0"
                    });
                }

                termino = termino.Trim();

                // Consulta base: busca por Nombre (paridad con BusquedaUsuario -> Contains)
                IQueryable<Autor> query = _context.Autors
                    .Where(a => a.Nombre.Contains(termino));

                // Total de coincidencias
                var totalResultados = await query.CountAsync();

                // Paginación + mapeo a DTO
                var autores = await query
                    .OrderBy(a => a.IdAutor)
                    .Select(a => new AutorDto
                    {
                        IdAutor = a.IdAutor,
                        Nombre = a.Nombre
                    })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                response.Success = true;
                response.Data = autores;
                response.Pagination = new PaginationInfo
                {
                    PaginaActual = pagina,
                    ResultadosPorPagina = resultadoPorPagina,
                    TotalResultados = totalResultados,
                    TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadoPorPagina)
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }



        // GET /Autor/ListarViewAutor
        [Authorize(Policy = "StaffOnly")]

        [HttpGet("ListarViewAutor")]
        public async Task<ActionResult<PaginacionResponse<AutorDto>>> ListarViewAutor(int pagina = 1, int resultadoPorPagina = 10)
        {
            var response = new PaginacionResponse<AutorDto>();

            try
            {
                if (pagina < 1 || resultadoPorPagina < 1)
                {
                    return BadRequest(new PaginacionResponse<AutorDto>
                    {
                        Success = false,
                        Message = "La página y resultados por página deben ser mayores a 0"
                    });
                }

                // Consulta base
                IQueryable<Autor> query = _context.Autors;

                // Total de registros
                var totalResultados = await query.CountAsync();

                // Paginación y mapeo a DTO
                var autores = await query
                    .OrderBy(a => a.IdAutor)
                    .Select(a => new AutorDto
                    {
                        IdAutor = a.IdAutor,
                        Nombre = a.Nombre
                    })
                    .Skip((pagina - 1) * resultadoPorPagina)
                    .Take(resultadoPorPagina)
                    .ToListAsync();

                // Construir respuesta
                response.Success = true;
                response.Data = autores;
                response.Pagination = new PaginationInfo
                {
                    PaginaActual = pagina,
                    ResultadosPorPagina = resultadoPorPagina,
                    TotalResultados = totalResultados,
                    TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadoPorPagina)
                };
                response.Message = "Lista de autores obtenida exitosamente";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error interno del servidor: " + ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }



        [Authorize(Policy = "StaffOnly")]
        [HttpPost("Registro")]
        public async Task<ActionResult<ApiResponse>> Registro([FromBody] AutorDto registro)
        {
            var api = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    api.Success = false;
                    api.Message = "Datos de registro inválidos";
                    api.Data = new { errores = ModelState.Values.SelectMany(v => v.Errors) };
                    return BadRequest(api);
                }

                if (string.IsNullOrWhiteSpace(registro.Nombre))
                {
                    api.Success = false;
                    api.Message = "El nombre es requerido";
                    return BadRequest(api);
                }

                string nombre = registro.Nombre.Trim();

                var autorExistente = await _context.Autors
                    .FirstOrDefaultAsync(a => a.Nombre.ToLower() == nombre.ToLower());

                if (autorExistente != null)
                {
                    api.Success = false;
                    api.Message = "Ya existe un autor con ese nombre";
                    return Conflict(api);
                }

                var nuevoAutor = new Autor
                {
                    Nombre = nombre
                };

                _context.Autors.Add(nuevoAutor);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Autor registrado exitosamente";
                api.Data = new { idAutor = nuevoAutor.IdAutor };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor";
                api.Data = new { error = ex.Message };
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpPut("Editar")]
        public async Task<ActionResult<ApiResponse>> EditarAutor([FromBody] AutorDto editarDto)
        {
            var api = new ApiResponse();
            try
            {
                if (!ModelState.IsValid)
                {
                    api.Success = false;
                    api.Message = "Datos de edición inválidos";
                    api.Data = new { errores = ModelState.Values.SelectMany(v => v.Errors) };
                    return BadRequest(api);
                }

                if (editarDto.IdAutor <= 0)
                {
                    api.Success = false;
                    api.Message = "El ID del autor es requerido";
                    return BadRequest(api);
                }

                var autor = await _context.Autors
                    .FirstOrDefaultAsync(a => a.IdAutor == editarDto.IdAutor);

                if (autor == null)
                {
                    api.Success = false;
                    api.Message = "Autor no encontrado";
                    return NotFound(api);
                }

                if (!string.IsNullOrWhiteSpace(editarDto.Nombre))
                {
                    string nuevoNombre = editarDto.Nombre.Trim();

                    var duplicado = await _context.Autors
                        .AnyAsync(a => a.IdAutor != editarDto.IdAutor &&
                                       a.Nombre.ToLower() == nuevoNombre.ToLower());

                    if (duplicado)
                    {
                        api.Success = false;
                        api.Message = "Ya existe otro autor con ese nombre";
                        return Conflict(api);
                    }

                    autor.Nombre = nuevoNombre;
                }

                _context.Autors.Update(autor);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Autor actualizado exitosamente";
                api.Data = new { idAutor = autor.IdAutor };
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor";
                api.Data = new { error = ex.Message };
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpDelete("Eliminar")]
        public async Task<ActionResult<ApiResponse>> EliminarAutor(int id)
        {
            var api = new ApiResponse();
            try
            {
                var autor = await _context.Autors
                    .Include(a => a.IdLibros)
                    .FirstOrDefaultAsync(a => a.IdAutor == id);

                if (autor == null)
                {
                    api.Success = false;
                    api.Message = "Autor no encontrado";
                    return NotFound(api);
                }

                // Evita eliminar si tiene libros asociados
                if (autor.IdLibros.Any())
                {
                    api.Success = false;
                    api.Message = "No se puede eliminar: el autor tiene libros asociados";
                    return Conflict(api);
                }

                _context.Autors.Remove(autor);
                await _context.SaveChangesAsync();

                api.Success = true;
                api.Message = "Autor eliminado exitosamente";
                return Ok(api);
            }
            catch (Exception ex)
            {
                api.Success = false;
                api.Message = "Error interno del servidor";
                api.Data = new { error = ex.Message };
                return StatusCode(500, api);
            }
        }

        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("Get-autor")]
        public async Task<ActionResult<int>> GetAutor(string nombre)
        {
            try
            {
                var autor = await _context.Autors.FirstOrDefaultAsync(x => x.Nombre == nombre);
                if (autor == null)
                {
                    return NotFound(new { message = "Autor no encontrado" });
                }
                var idAutor = autor.IdAutor;
                return Ok(idAutor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el autor: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }

        }


    }
}
