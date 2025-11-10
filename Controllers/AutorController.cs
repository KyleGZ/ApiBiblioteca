using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
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
                    .Skip((pagina - 1) * resultadoPorPagina) // ✅ Skip correcto
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


        // GET /Autor/Listar
        //[HttpGet("Listar")]
        //public async Task<IActionResult> ListarAutores()
        //{
        //    try
        //    {
        //        var autores = await _context.Autors
        //            .Select(a => new AutorDto
        //            {
        //                IdAutor = a.IdAutor,
        //                Nombre = a.Nombre
        //            })
        //            .OrderBy(a => a.IdAutor)
        //            .ToListAsync();

        //        return Ok(new
        //        {
        //            mensaje = "Lista de autores obtenida exitosamente",
        //            totalAutores = autores.Count,
        //            autores = autores
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            mensaje = "Error interno del servidor",
        //            error = ex.Message
        //        });
        //    }
        //}

        // GET /Autor/ListarViewAutor
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


        //Autor/Registro
        [HttpPost("Registro")]
        public async Task<IActionResult> Registro([FromBody] AutorDto registro)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        mensaje = "Datos de registro inválidos",
                        errores = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }

                if (string.IsNullOrWhiteSpace(registro.Nombre))
                {
                    return BadRequest(new { mensaje = "El nombre es requerido" });
                }

                string nombre = registro.Nombre.Trim();

                var autorExistente = await _context.Autors
                    .FirstOrDefaultAsync(a => a.Nombre.ToLower() == nombre.ToLower());

                if (autorExistente != null)
                {
                    return Conflict(new { mensaje = "Ya existe un autor con ese nombre" });
                }

                var nuevoAutor = new Autor
                {
                    Nombre = nombre
                };

                _context.Autors.Add(nuevoAutor);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Autor registrado exitosamente",
                    idAutor = nuevoAutor.IdAutor
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        //Autor/Editar
        [HttpPut("Editar")]
        public async Task<IActionResult> EditarAutor([FromBody] AutorDto editarDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        mensaje = "Datos de edición inválidos",
                        errores = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }

                if (editarDto.IdAutor <= 0)
                {
                    return BadRequest(new { mensaje = "El ID del autor es requerido" });
                }

                var autor = await _context.Autors
                    .FirstOrDefaultAsync(a => a.IdAutor == editarDto.IdAutor);

                if (autor == null)
                {
                    return NotFound(new { mensaje = "Autor no encontrado" });
                }

                if (!string.IsNullOrWhiteSpace(editarDto.Nombre))
                {
                    string nuevoNombre = editarDto.Nombre.Trim();

                    var duplicado = await _context.Autors
                        .AnyAsync(a => a.IdAutor != editarDto.IdAutor &&
                                       a.Nombre.ToLower() == nuevoNombre.ToLower());

                    if (duplicado)
                    {
                        return Conflict(new { mensaje = "Ya existe otro autor con ese nombre" });
                    }

                    autor.Nombre = nuevoNombre;
                }

                _context.Autors.Update(autor);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Autor actualizado exitosamente",
                    idAutor = autor.IdAutor
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        //Autor/Eliminar?id=5
        [HttpDelete("Eliminar")]
        public async Task<IActionResult> EliminarAutor(int id)
        {
            try
            {
                var autor = await _context.Autors
                    .Include(a => a.IdLibros)
                    .FirstOrDefaultAsync(a => a.IdAutor == id);

                if (autor == null)
                {
                    return NotFound(new { mensaje = "Autor no encontrado" });
                }

                // Evita eliminar si tiene libros asociados
                if (autor.IdLibros.Any())
                {
                    return Conflict(new { mensaje = "No se puede eliminar: el autor tiene libros asociados" });
                }

                _context.Autors.Remove(autor);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Autor eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }
    }
}
