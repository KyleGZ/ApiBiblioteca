using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiBiblioteca.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class LibroController : Controller
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;
        private readonly ILibroImportService _libroImportService;

        public LibroController(DbContextBiblioteca dbContext, IAutorizacionService autorizacionService, ILibroImportService libroImportService)
        {
            _autorizacionService = autorizacionService;
            _context = dbContext;
            _libroImportService = libroImportService;
        }

        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("ListaView")]
        public async Task<PaginacionResponse<LibroListaView>> ListaView(int pagina = 1, int resultadosPorPagina = 20)
        {
            var response = new PaginacionResponse<LibroListaView>();

            try
            {
                var query = _context.Libros
                    .Include(l => l.IdEditorialNavigation) // Editorial
                    .Include(l => l.IdAutors)              // Autores (N:N)
                    .Include(l => l.IdGeneros)             // Géneros (N:N)
                    .AsQueryable();

                // Obtener total de resultados
                var totalResultados = await query.CountAsync();

                // Aplicar paginación y mapear a LibroListaView
                var libros = await query
                    .OrderBy(l => l.Titulo)
                    .Skip((pagina - 1) * resultadosPorPagina)
                    .Take(resultadosPorPagina)
                    .Select(l => new LibroListaView
                    {
                        IdLibro = l.IdLibro,
                        Titulo = l.Titulo,
                        ISBN = l.Isbn,
                        Editorial = l.IdEditorialNavigation.Nombre,
                        Autor = l.IdAutors.Select(a => a.Nombre).ToList(),
                        Genero = l.IdGeneros.Select(g => g.Nombre).ToList(),
                        Estado = l.Estado,
                        PortadaUrl = l.PortadaUrl
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = libros;
                response.Pagination = new PaginationInfo
                {
                    PaginaActual = pagina,
                    ResultadosPorPagina = resultadosPorPagina,
                    TotalResultados = totalResultados,
                    TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadosPorPagina)
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al obtener libros: {ex.Message}";
            }

            return response;
        }



        /*
         * Metodo para realizar busquedas por descripcion
         */
        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("Busqueda-Descripcion")]
        public async Task<PaginacionResponse<LibroListaView>> BuscarPorDescripcionAsync(
    string terminoBusqueda,
    int pagina = 1,
    int resultadosPorPagina = 12)
        {
            var response = new PaginacionResponse<LibroListaView>();

            try
            {
                var query = _context.Libros
                    .Include(l => l.IdEditorialNavigation)
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdGeneros)
                    .AsQueryable();

                // Búsqueda SOLO por descripción usando LIKE
                if (!string.IsNullOrEmpty(terminoBusqueda))
                {
                    query = query.Where(l =>
                    EF.Functions.Like(l.Descripcion, $"%{terminoBusqueda}%")
                    );
                }

                // Obtener total de resultados
                var totalResultados = await query.CountAsync();

                // Aplicar paginación y mapear a LibroListaView
                var libros = await query
                    .OrderBy(l => l.Titulo)
                    .Skip((pagina - 1) * resultadosPorPagina)
                    .Take(resultadosPorPagina)
                    .Select(l => new LibroListaView
                    {
                        IdLibro = l.IdLibro,
                        Titulo = l.Titulo,
                        ISBN = l.Isbn,
                        Editorial = l.IdEditorialNavigation.Nombre,
                        Estado = l.Estado,
                        PortadaUrl = l.PortadaUrl,
                        Autor = l.IdAutors
                            .Select(a => a.Nombre)
                            .ToList(),
                        Genero = l.IdGeneros
                            .Select(g => g.Nombre)
                            .ToList()
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = libros;
                response.Pagination = new PaginationInfo
                {
                    PaginaActual = pagina,
                    ResultadosPorPagina = resultadosPorPagina,
                    TotalResultados = totalResultados,
                    TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadosPorPagina)
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al buscar libros por descripción: {ex.Message}";
            }

            return response;
        }

        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("buscar-rapida")]
        public async Task<ActionResult<PaginacionResponse<LibroListaView>>> BuscarRapida(
    string termino,
    int pagina = 1,
    int resultadosPorPagina = 20)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(termino))
                {
                    return BadRequest(new PaginacionResponse<LibroListaView>
                    {
                        Success = false,
                        Message = "El término de búsqueda no puede estar vacío"
                    });
                }

                if (pagina < 1 || resultadosPorPagina < 1)
                {
                    return BadRequest(new PaginacionResponse<LibroListaView>
                    {
                        Success = false,
                        Message = "La página y resultados por página deben ser mayores a 0"
                    });
                }

                termino = termino.Trim();
                IQueryable<Libro> query = null;
                string tipoBusqueda = "";

                // Paso 1: Buscar por ISBN exacto
                var resultadosISBN = await _context.Libros
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdEditorialNavigation)
                    .Include(l => l.IdGeneros)
                    .Where(l => l.Isbn == termino)
                    .Select(l => new LibroListaView
                    {
                        IdLibro = l.IdLibro,
                        Titulo = l.Titulo,
                        ISBN = l.Isbn,
                        Autor = l.IdAutors.Select(a => a.Nombre).ToList(),
                        Editorial = l.IdEditorialNavigation.Nombre,
                        Genero = l.IdGeneros.Select(g => g.Nombre).ToList(),
                        Estado = l.Estado,
                        PortadaUrl = l.PortadaUrl
                    })
                    .ToListAsync();

                if (resultadosISBN.Any())
                {
                    tipoBusqueda = "isbn";
                    return PaginarResultados(resultadosISBN, pagina, resultadosPorPagina, tipoBusqueda);
                }

                // Paso 2: Buscar en título
                var queryTitulo = _context.Libros
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdEditorialNavigation)
                    .Include(l => l.IdGeneros)
                    .Where(l => l.Titulo.Contains(termino))
                    .Select(l => new LibroListaView
                    {
                        IdLibro = l.IdLibro,
                        Titulo = l.Titulo,
                        ISBN = l.Isbn,
                        Autor = l.IdAutors.Select(a => a.Nombre).ToList(),
                        Editorial = l.IdEditorialNavigation.Nombre,
                        Genero = l.IdGeneros.Select(g => g.Nombre).ToList(),
                        Estado = l.Estado,
                        PortadaUrl = l.PortadaUrl
                    });

                var totalTitulo = await queryTitulo.CountAsync();

                if (totalTitulo > 0)
                {
                    tipoBusqueda = "título";
                    var resultadosTitulo = await queryTitulo
                        .Skip((pagina - 1) * resultadosPorPagina)
                        .Take(resultadosPorPagina)
                        .ToListAsync();

                    return new PaginacionResponse<LibroListaView>
                    {
                        Success = true,
                        Message = $"Se encontraron {totalTitulo} resultados en títulos",
                        Data = resultadosTitulo,
                        Pagination = new PaginationInfo
                        {
                            PaginaActual = pagina,
                            TotalPaginas = (int)Math.Ceiling(totalTitulo / (double)resultadosPorPagina),
                            TotalResultados = totalTitulo,
                            ResultadosPorPagina = resultadosPorPagina
                        }
                    };
                }

                // Paso 3: Buscar en autor
                var queryAutor = _context.Libros
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdEditorialNavigation)
                    .Include(l => l.IdGeneros)
                    .Where(l => l.IdAutors.Any(a => a.Nombre.Contains(termino)))
                    .Select(l => new LibroListaView
                    {
                        IdLibro = l.IdLibro,
                        Titulo = l.Titulo,
                        ISBN = l.Isbn,
                        Autor = l.IdAutors.Select(a => a.Nombre).ToList(),
                        Editorial = l.IdEditorialNavigation.Nombre,
                        Genero = l.IdGeneros.Select(g => g.Nombre).ToList(),
                        Estado = l.Estado,
                        PortadaUrl = l.PortadaUrl
                    });

                var totalAutor = await queryAutor.CountAsync();

                if (totalAutor > 0)
                {
                    tipoBusqueda = "autor";
                    var resultadosAutor = await queryAutor
                        .Skip((pagina - 1) * resultadosPorPagina)
                        .Take(resultadosPorPagina)
                        .ToListAsync();

                    return new PaginacionResponse<LibroListaView>
                    {
                        Success = true,
                        Message = $"Se encontraron {totalAutor} resultados en autores",
                        Data = resultadosAutor,
                        Pagination = new PaginationInfo
                        {
                            PaginaActual = pagina,
                            TotalPaginas = (int)Math.Ceiling(totalAutor / (double)resultadosPorPagina),
                            TotalResultados = totalAutor,
                            ResultadosPorPagina = resultadosPorPagina
                        }
                    };
                }

                // No se encontraron resultados
                return Ok(new PaginacionResponse<LibroListaView>
                {
                    Success = false,
                    Message = $"No se encontraron libros que coincidan con '{termino}'",
                    Data = new List<LibroListaView>(),
                    Pagination = new PaginationInfo
                    {
                        PaginaActual = pagina,
                        TotalPaginas = 0,
                        TotalResultados = 0,
                        ResultadosPorPagina = resultadosPorPagina
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en búsqueda rápida: {ex.Message}");
                return StatusCode(500, new PaginacionResponse<LibroListaView>
                {
                    Success = false,
                    Message = "Error interno del servidor al realizar la búsqueda"
                });
            }
        }

        // Método auxiliar para paginar resultados de ISBN (que ya están en memoria)
        private PaginacionResponse<LibroListaView> PaginarResultados(
            List<LibroListaView> resultados,
            int pagina,
            int resultadosPorPagina,
            string tipoBusqueda)
        {
            var totalResultados = resultados.Count;
            var resultadosPaginados = resultados
                .Skip((pagina - 1) * resultadosPorPagina)
                .Take(resultadosPorPagina)
                .ToList();

            return new PaginacionResponse<LibroListaView>
            {
                Success = true,
                Message = $"Se encontraron {totalResultados} resultados en {tipoBusqueda}",
                Data = resultadosPaginados,
                Pagination = new PaginationInfo
                {
                    PaginaActual = pagina,
                    TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadosPorPagina),
                    TotalResultados = totalResultados,
                    ResultadosPorPagina = resultadosPorPagina
                }
            };
        }


        /*
         * Metodo para registrar nuevos libros
         */
        [Authorize(Policy = "StaffOnly")]
        [HttpPost("Registro-Libro")]
        public async Task<ActionResult<ApiResponse>> Registro([FromBody] CrearLibroDto libroDto)
        {
            try
            {
                // Se valida que el modelo este completo
                if (string.IsNullOrWhiteSpace(libroDto.Titulo))
                    return BadRequest(new ApiResponse { Success = false, Message = "El título es requerido" });

                if (string.IsNullOrWhiteSpace(libroDto.Isbn))
                    return BadRequest(new ApiResponse { Success = false, Message = "El ISBN es requerido" });

                if (libroDto.IdAutores == null || !libroDto.IdAutores.Any())
                    return BadRequest(new ApiResponse { Success = false, Message = "Debe asignar al menos un autor" });

                // Se verifca que no exista un libro con el mismo ISBN
                var libroExistente = await _context.Libros
                    .FirstOrDefaultAsync(l => l.Isbn == libroDto.Isbn);

                if (libroExistente != null)
                    return BadRequest(new ApiResponse { Success = false, Message = "Ya existe un libro con este ISBN" });

                var autores = await _context.Autors
                    .Where(a => libroDto.IdAutores.Contains(a.IdAutor))
                    .ToListAsync();

                if (autores.Count != libroDto.IdAutores.Count)
                    return BadRequest(new ApiResponse { Success = false, Message = "Uno o más autores no existen" });

                // Se valida que la lista de géneros sea válida si se proporciona
                List<Genero>? generos = null;
                if (libroDto.IdGeneros != null && libroDto.IdGeneros.Any())
                {
                    generos = await _context.Generos
                        .Where(g => libroDto.IdGeneros.Contains(g.IdGenero))
                        .ToListAsync();

                    if (generos.Count != libroDto.IdGeneros.Count)
                        return BadRequest(new ApiResponse { Success = false, Message = "Uno o más géneros no existen" });
                }




                // Se crea el nuevo libro
                var libro = new Libro
                {
                    Titulo = libroDto.Titulo.Trim(),
                    Isbn = libroDto.Isbn.Trim(),

                    IdEditorial = libroDto.IdEditorial,
                    IdSeccion = libroDto.IdSeccion,
                    Estado = libroDto.Estado ?? "disponible",
                    Descripcion = libroDto.Descripcion?.Trim() ?? "",
                    PortadaUrl = libroDto.PortadaUrl?.Trim() ?? "/imagenes/portadas/default-book-cover.jpg",
                    IdAutors = autores,
                    IdGeneros = generos ?? new List<Genero>()
                };

                // Se guarda el libro en la base de datos
                _context.Libros.Add(libro);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Libro creado exitosamente",
                    Data = new { IdLibro = libro.IdLibro }
                });
            }
            catch (Exception ex)
            {


                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al crear el libro: {ex.Message}"
                });
            }
        }


        /*
         * Metodo para editar un libro
         */
        [Authorize(Policy = "StaffOnly")]
        [HttpPut("Editar-Libro")]
        public async Task<ActionResult<ApiResponse>> EditarLibro(int idLibro, [FromBody] CrearLibroDto editarLibroDto)
        {
            try
            {
                // Validaciones básicas del modelo
                if (string.IsNullOrWhiteSpace(editarLibroDto.Titulo))
                    return BadRequest(new ApiResponse { Success = false, Message = "El título es requerido" });

                if (string.IsNullOrWhiteSpace(editarLibroDto.Isbn))
                    return BadRequest(new ApiResponse { Success = false, Message = "El ISBN es requerido" });

                if (editarLibroDto.IdAutores == null || !editarLibroDto.IdAutores.Any())
                    return BadRequest(new ApiResponse { Success = false, Message = "Debe asignar al menos un autor" });

                // Buscar el libro existente
                var libroExistente = await _context.Libros
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdGeneros)
                    .FirstOrDefaultAsync(l => l.IdLibro == idLibro);

                if (libroExistente == null)
                    return NotFound(new ApiResponse { Success = false, Message = "Libro no encontrado" });

                // Verificar si el ISBN ya existe en OTRO libro (para evitar duplicados)
                var libroConMismoIsbn = await _context.Libros
                    .FirstOrDefaultAsync(l => l.Isbn == editarLibroDto.Isbn && l.IdLibro != idLibro);

                if (libroConMismoIsbn != null)
                    return BadRequest(new ApiResponse { Success = false, Message = "Ya existe otro libro con este ISBN" });

                // Validar autores
                var autores = await _context.Autors
                    .Where(a => editarLibroDto.IdAutores.Contains(a.IdAutor))
                    .ToListAsync();

                if (autores.Count != editarLibroDto.IdAutores.Count)
                    return BadRequest(new ApiResponse { Success = false, Message = "Uno o más autores no existen" });

                // Validar géneros si se proporcionan
                List<Genero>? generos = null;
                if (editarLibroDto.IdGeneros != null && editarLibroDto.IdGeneros.Any())
                {
                    generos = await _context.Generos
                        .Where(g => editarLibroDto.IdGeneros.Contains(g.IdGenero))
                        .ToListAsync();

                    if (generos.Count != editarLibroDto.IdGeneros.Count)
                        return BadRequest(new ApiResponse { Success = false, Message = "Uno o más géneros no existen" });
                }

                // Actualizar el libro existente
                libroExistente.Titulo = editarLibroDto.Titulo.Trim();
                libroExistente.Isbn = editarLibroDto.Isbn.Trim();
                libroExistente.IdEditorial = editarLibroDto.IdEditorial;
                libroExistente.IdSeccion = editarLibroDto.IdSeccion;
                libroExistente.Estado = editarLibroDto.Estado ?? libroExistente.Estado; // Mantener estado actual si no se proporciona
                libroExistente.Descripcion = editarLibroDto.Descripcion?.Trim() ?? libroExistente.Descripcion;

                // Actualizar portada solo si se proporciona una nueva
                if (!string.IsNullOrWhiteSpace(editarLibroDto.PortadaUrl))
                {
                    libroExistente.PortadaUrl = editarLibroDto.PortadaUrl.Trim();
                }

                // Actualizar relaciones muchos-a-muchos
                libroExistente.IdAutors = autores;
                libroExistente.IdGeneros = generos ?? new List<Genero>();

                // Marcar como modificado y guardar
                _context.Libros.Update(libroExistente);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Libro actualizado exitosamente",
                    Data = new { IdLibro = libroExistente.IdLibro }
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error de concurrencia al actualizar el libro. Otro usuario puede haber modificado el registro."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al actualizar el libro: {ex.Message}"
                });
            }
        }



        /*
         * Este metodo retorna un libro con la informacion lista para actualizarse
         */
        [Authorize(Policy = "StaffOnly")]
        [HttpGet("ObtenerLibro-Editar")]
        public async Task<ActionResult<ApiResponse>> ObtenerLibroParaEditar(int idLibro)
        {
            try
            {
                var libro = await _context.Libros
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdGeneros)
                    .Include(l => l.IdEditorialNavigation)
                    .Include(l => l.IdSeccionNavigation)
                    .FirstOrDefaultAsync(l => l.IdLibro == idLibro);

                if (libro == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Libro no encontrado"
                    });
                }

                var libroParaEditar = new ObtenerLibroEditar
                {
                    IdLibro = libro.IdLibro,
                    Titulo = libro.Titulo,
                    ISBN = libro.Isbn,
                    EditorialId = libro.IdEditorial,
                    SeccionId = libro.IdSeccion,
                    Estado = libro.Estado,
                    Descripcion = libro.Descripcion ?? "",
                    PortadaUrl = libro.PortadaUrl ?? "",
                    Autores = libro.IdAutors.Select(a => new AutorChipDto
                    {
                        Id = a.IdAutor,
                        Nombre = a.Nombre
                    }).ToList(),
                    Generos = libro.IdGeneros.Select(g => new GeneroChipDto
                    {
                        Id = g.IdGenero,
                        Nombre = g.Nombre
                    }).ToList()
                };

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = libroParaEditar,
                    Message = "Libro obtenido exitosamente para editar"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al obtener el libro para editar: {ex.Message}"
                });
            }
        }

        [Authorize(Policy = "StaffOnly")]
        [HttpGet("buscar-editorial")]
        public async Task<ActionResult<ApiResponse>> BuscarEditorial(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return BadRequest(new ApiResponse { Success = false, Message = "Debe proporcionar un nombre de editorial" });

                // Buscar insensible a mayúsculas/minúsculas y espacios
                var editorial = await _context.Editorials
                    .FirstOrDefaultAsync(e => EF.Functions.Like(e.Nombre.Trim(), nombre.Trim()));

                if (editorial == null)
                    return NotFound(new ApiResponse { Success = false, Message = $"No existe una editorial con el nombre '{nombre}'" });

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Editorial encontrada",
                    Data = editorial
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al buscar la editorial: {ex.Message}"
                });
            }
        }

        /*
         * Metodo para obtener los detalles de un libro
         */
        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("Detalle-Libro")]
        public async Task<ActionResult<LibroDetalleDto>> DetalleLibro(int idLibro)
        {
            try
            {
                var libro = await _context.Libros
                    .Include(l => l.IdAutors)
                    .Include(l => l.IdGeneros)
                    .Include(l => l.IdEditorialNavigation)
                    .Include(l => l.IdSeccionNavigation)
                    .FirstOrDefaultAsync(l => l.IdLibro == idLibro);
                if (libro == null)
                    return NotFound(new { message = "Libro no encontrado" });
                var libroDetalle = new LibroDetalleDto
                {
                    IdLibro = libro.IdLibro,
                    Isbn = libro.Isbn,
                    Titulo = libro.Titulo,
                    Autor = libro.IdAutors.Select(a => a.Nombre).ToList(),
                    Genero = libro.IdGeneros.Select(g => g.Nombre).ToList(),
                    Editorial = libro.IdEditorialNavigation?.Nombre ?? "Desconocida",
                    Seccion = libro.IdSeccionNavigation?.Nombre ?? "Desconocida",
                    Descripcion = libro.Descripcion ?? "Desconocida",
                    Portada = libro.PortadaUrl,
                    Estado = libro.Estado
                };
                return Ok(libroDetalle);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener detalles del libro: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }

        }


        /*
         * Metodo para obtener la plantilla de importacion de libros
         */
        [Authorize(Policy = "StaffOnly")]
        [HttpGet("Plantilla-Importacion")]
        public async Task<IActionResult> DescargarPlantilla()
        {
            var contenido = await _libroImportService.GenerarPlantillaExcelAsync();

            return File(
                contenido,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Plantilla_Importacion_Libros.xlsx"
            );
        }

        /*
         * Metodo para importar libros desde un archivo Excel
         */
        [Authorize(Policy = "StaffOnly")]
        [HttpPost("Importar-Libro")]
        public async Task<IActionResult> ImportarLibros(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("Debe adjuntar un archivo Excel válido.");

            try
            {
                var resultado = await _libroImportService.ImportarLibrosDesdeExcelAsync(archivo);

                if (resultado.Success)
                {
                    return Ok(new
                    {
                        mensaje = resultado.Message,
                        datos = resultado.Data
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        mensaje = resultado.Message,
                        datos = resultado.Data
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Ocurrió un error durante la importación.",
                    error = ex.Message
                });
            }
        }

        //*
        [Authorize(Policy = "GeneralPolicy")]
        [HttpGet("Get-libro")]
        public async Task<ActionResult<int>> GetLibro(string isbn)
        {
            try
            {
                var libro = await _context.Libros.FirstOrDefaultAsync(x => x.Isbn == isbn);
                if (libro == null)
                {
                    return NotFound(new { message = "Libro no encontrado" });
                }
                var idLibro = libro.IdLibro;
                return Ok(idLibro);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el libro: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }

        }
    }
}
