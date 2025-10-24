using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiBiblioteca.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class LibroController : Controller
    {
        private readonly DbContextBiblioteca _context;
        private readonly IAutorizacionService _autorizacionService;

        public LibroController(DbContextBiblioteca dbContext, IAutorizacionService autorizacionService)
        {
            _autorizacionService = autorizacionService;
            _context = dbContext;
        }



        //[HttpGet("ListaView")]
        //public async Task<ActionResult<List<LibroListaView>>> ListaView()
        //{
        //    try
        //    {
        //        // CORREGIR: Tienes un error aquí - Editorial está usando IdAutorNavigation
        //        var libros = await _context.Libros
        //            .Include(l => l.IdAutorNavigation)
        //            .Include(l => l.IdEditorialNavigation)    // Incluir datos de editorial
        //            .Include(l => l.IdGeneroNavigation)
        //            .Select(l => new LibroListaView
        //            {
        //                IdLibro = l.IdLibro,
        //                Titulo = l.Titulo,
        //                ISBN = l.Isbn,
        //                Autor = l.IdAutorNavigation.Nombre,
        //                Editorial = l.IdEditorialNavigation.Nombre, // CORREGIDO: usar IdEditorialNavigation
        //                Genero = l.IdGeneroNavigation.Nombre,
        //                Estado = l.Estado,
        //                PortadaUrl = l.PortadaUrl
        //            })
        //            .ToListAsync();

        //        if (libros == null || !libros.Any())
        //        {
        //            return Ok(new List<LibroListaView>());
        //        }

        //        return Ok(libros); // Devuelve JSON
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error al obtener libros: {ex.Message}");
        //        return StatusCode(500, new { error = "Error interno del servidor" });
        //    }
        //}

        [HttpGet("ListaView")]
        public async Task<ActionResult<List<LibroListaView>>> ListaView()
        {
            try
            {
                var libros = await _context.Libros
                    .Include(l => l.IdEditorialNavigation) // Editorial
                    .Include(l => l.IdAutors)              // Autores (N:N)
                    .Include(l => l.IdGeneros)             // Géneros (N:N)
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

                if (libros == null || !libros.Any())
                    return Ok(new List<LibroListaView>());

                return Ok(libros);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener libros: {ex.Message}");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }




        /*
         * Metodo para realizar busquedas especificas por libro
         */


        //    [HttpGet("buscar-rapida")]
        //    public async Task<ActionResult<PaginacionResponse>> BuscarRapida(
        //string termino,
        //int pagina = 1,
        //int resultadosPorPagina = 20)
        //    {
        //        try
        //        {
        //            // Validaciones
        //            if (string.IsNullOrWhiteSpace(termino))
        //            {
        //                return BadRequest(new PaginacionResponse
        //                {
        //                    Success = false,
        //                    Message = "El término de búsqueda no puede estar vacío"
        //                });
        //            }

        //            if (pagina < 1 || resultadosPorPagina < 1)
        //            {
        //                return BadRequest(new PaginacionResponse
        //                {
        //                    Success = false,
        //                    Message = "La página y resultados por página deben ser mayores a 0"
        //                });
        //            }

        //            termino = termino.Trim();
        //            IQueryable<Libro> query = null;
        //            string tipoBusqueda = "";

        //            // Paso 1: Buscar por ISBN exacto
        //            var resultadosISBN = await _context.Libros
        //                .Include(l => l.IdAutorNavigation)
        //                .Include(l => l.IdEditorialNavigation)
        //                .Include(l => l.IdGeneroNavigation)
        //                .Where(l => l.Isbn == termino)
        //                .Select(l => new LibroListaView
        //                {
        //                    IdLibro = l.IdLibro,
        //                    Titulo = l.Titulo,
        //                    ISBN = l.Isbn,
        //                    Autor = l.IdAutorNavigation.Nombre,
        //                    Editorial = l.IdEditorialNavigation.Nombre,
        //                    Genero = l.IdGeneros.Select(g => g.Nombre).ToList(),
        //                    Estado = l.Estado,
        //                    PortadaUrl = l.PortadaUrl
        //                })
        //                .ToListAsync();

        //            if (resultadosISBN.Any())
        //            {
        //                tipoBusqueda = "isbn";
        //                return PaginarResultados(resultadosISBN, pagina, resultadosPorPagina, tipoBusqueda);
        //            }

        //            // Paso 2: Buscar en título
        //            var queryTitulo = _context.Libros
        //                .Include(l => l.IdAutorNavigation)
        //                .Include(l => l.IdEditorialNavigation)
        //                .Include(l => l.IdGeneroNavigation)
        //                .Where(l => l.Titulo.Contains(termino))
        //                .Select(l => new LibroListaView
        //                {
        //                    IdLibro = l.IdLibro,
        //                    Titulo = l.Titulo,
        //                    ISBN = l.Isbn,
        //                    Autor = l.IdAutorNavigation.Nombre,
        //                    Editorial = l.IdEditorialNavigation.Nombre,
        //                    Genero = l.IdGeneroNavigation.Nombre,
        //                    Estado = l.Estado,
        //                    PortadaUrl = l.PortadaUrl
        //                });

        //            var totalTitulo = await queryTitulo.CountAsync();

        //            if (totalTitulo > 0)
        //            {
        //                tipoBusqueda = "título";
        //                var resultadosTitulo = await queryTitulo
        //                    .Skip((pagina - 1) * resultadosPorPagina)
        //                    .Take(resultadosPorPagina)
        //                    .ToListAsync();

        //                return new PaginacionResponse
        //                {
        //                    Success = true,
        //                    Message = $"Se encontraron {totalTitulo} resultados en títulos",
        //                    Data = resultadosTitulo,
        //                    Pagination = new PaginationInfo
        //                    {
        //                        PaginaActual = pagina,
        //                        TotalPaginas = (int)Math.Ceiling(totalTitulo / (double)resultadosPorPagina),
        //                        TotalResultados = totalTitulo,
        //                        ResultadosPorPagina = resultadosPorPagina
        //                    }
        //                };
        //            }

        //            // Paso 3: Buscar en autor
        //            var queryAutor = _context.Libros
        //                .Include(l => l.IdAutorNavigation)
        //                .Include(l => l.IdEditorialNavigation)
        //                .Include(l => l.IdGeneroNavigation)
        //                .Where(l => l.IdAutorNavigation.Nombre.Contains(termino))
        //                .Select(l => new LibroListaView
        //                {
        //                    IdLibro = l.IdLibro,
        //                    Titulo = l.Titulo,
        //                    ISBN = l.Isbn,
        //                    Autor = l.IdAutorNavigation.Nombre,
        //                    Editorial = l.IdEditorialNavigation.Nombre,
        //                    Genero = l.IdGeneroNavigation.Nombre,
        //                    Estado = l.Estado,
        //                    PortadaUrl = l.PortadaUrl
        //                });

        //            var totalAutor = await queryAutor.CountAsync();

        //            if (totalAutor > 0)
        //            {
        //                tipoBusqueda = "autor";
        //                var resultadosAutor = await queryAutor
        //                    .Skip((pagina - 1) * resultadosPorPagina)
        //                    .Take(resultadosPorPagina)
        //                    .ToListAsync();

        //                return new PaginacionResponse
        //                {
        //                    Success = true,
        //                    Message = $"Se encontraron {totalAutor} resultados en autores",
        //                    Data = resultadosAutor,
        //                    Pagination = new PaginationInfo
        //                    {
        //                        PaginaActual = pagina,
        //                        TotalPaginas = (int)Math.Ceiling(totalAutor / (double)resultadosPorPagina),
        //                        TotalResultados = totalAutor,
        //                        ResultadosPorPagina = resultadosPorPagina
        //                    }
        //                };
        //            }

        //            // No se encontraron resultados
        //            return Ok(new PaginacionResponse
        //            {
        //                Success = false,
        //                Message = $"No se encontraron libros que coincidan con '{termino}'",
        //                Data = new List<LibroListaView>(),
        //                Pagination = new PaginationInfo
        //                {
        //                    PaginaActual = pagina,
        //                    TotalPaginas = 0,
        //                    TotalResultados = 0,
        //                    ResultadosPorPagina = resultadosPorPagina
        //                }
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error en búsqueda rápida: {ex.Message}");
        //            return StatusCode(500, new PaginacionResponse
        //            {
        //                Success = false,
        //                Message = "Error interno del servidor al realizar la búsqueda"
        //            });
        //        }
        //    }

        //    // Método auxiliar para paginar resultados de ISBN (que ya están en memoria)
        //    private PaginacionResponse PaginarResultados(
        //        List<LibroListaView> resultados,
        //        int pagina,
        //        int resultadosPorPagina,
        //        string tipoBusqueda)
        //    {
        //        var totalResultados = resultados.Count;
        //        var resultadosPaginados = resultados
        //            .Skip((pagina - 1) * resultadosPorPagina)
        //            .Take(resultadosPorPagina)
        //            .ToList();

        //        return new PaginacionResponse
        //        {
        //            Success = true,
        //            Message = $"Se encontraron {totalResultados} resultados en {tipoBusqueda}",
        //            Data = resultadosPaginados,
        //            Pagination = new PaginationInfo
        //            {
        //                PaginaActual = pagina,
        //                TotalPaginas = (int)Math.Ceiling(totalResultados / (double)resultadosPorPagina),
        //                TotalResultados = totalResultados,
        //                ResultadosPorPagina = resultadosPorPagina
        //            }
        //        };
        //    }

        /*
         * Metodo para realizar busquedas por descripcion
         */

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
                    EF.Functions.Like(l.Descripcion,$"%{terminoBusqueda}%")
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

        [HttpPost("Registro-Libro")]
        public async Task<ActionResult<ApiResponse>> Registro([FromBody] CrearLibroDto libroDto)
        {
            try
            {
                // ✅ 1. VALIDACIONES BÁSICAS PRIMERO
                if (string.IsNullOrWhiteSpace(libroDto.Titulo))
                    return BadRequest(new ApiResponse { Success = false, Message = "El título es requerido" });

                if (string.IsNullOrWhiteSpace(libroDto.Isbn))
                    return BadRequest(new ApiResponse { Success = false, Message = "El ISBN es requerido" });

                if (libroDto.IdAutores == null || !libroDto.IdAutores.Any())
                    return BadRequest(new ApiResponse { Success = false, Message = "Debe asignar al menos un autor" });

                // ✅ 2. VERIFICAR ISBN ÚNICO
                var libroExistente = await _context.Libros
                    .FirstOrDefaultAsync(l => l.Isbn == libroDto.Isbn);

                if (libroExistente != null)
                    return BadRequest(new ApiResponse { Success = false, Message = "Ya existe un libro con este ISBN" });

                // ✅ 3. CONSULTAR VALORES POR DEFECTO (TODO JUNTO ANTES DE LAS VALIDACIONES)
                var editorialPorDefecto = await _context.Editorials
                    .FirstOrDefaultAsync(e => e.Nombre.Trim().ToLower() == "editorial desconocida".ToLower());

                var seccionPorDefecto = await _context.Seccions
                    .FirstOrDefaultAsync(e => e.Nombre.Trim().ToLower() == "sección general".ToLower());

                // ✅ 4. VERIFICAR EXISTENCIA DE VALORES POR DEFECTO
                if (editorialPorDefecto == null)
                    return BadRequest(new ApiResponse { Success = false, Message = "Error del sistema: No existe la editorial por defecto" });

                if (seccionPorDefecto == null)
                    return BadRequest(new ApiResponse { Success = false, Message = "Error del sistema: No existe la sección por defecto" });

                //// ✅ 5. VALIDAR IDs PROPORCIONADOS (SI LOS HAY)
                //if (libroDto.IdEditorial.HasValue)
                //{
                //    var editorialExiste = await _context.Editorials
                //        .AnyAsync(e => e.IdEditorial == libroDto.IdEditorial.Value);
                //    if (!editorialExiste)
                //        return BadRequest(new ApiResponse { Success = false, Message = "La editorial seleccionada no existe" });
                //}

                //if (libroDto.IdSeccion.HasValue)
                //{
                //    var seccionExiste = await _context.Seccions
                //        .AnyAsync(s => s.IdSeccion == libroDto.IdSeccion.Value);
                //    if (!seccionExiste)
                //        return BadRequest(new ApiResponse { Success = false, Message = "La sección seleccionada no existe" });
                //}

                // ✅ 6. VALIDAR AUTORES
                var autores = await _context.Autors
                    .Where(a => libroDto.IdAutores.Contains(a.IdAutor))
                    .ToListAsync();

                if (autores.Count != libroDto.IdAutores.Count)
                    return BadRequest(new ApiResponse { Success = false, Message = "Uno o más autores no existen" });

                // ✅ 7. VALIDAR GÉNEROS (SI SE PROPORCIONAN)
                List<Genero>? generos = null;
                if (libroDto.IdGeneros != null && libroDto.IdGeneros.Any())
                {
                    generos = await _context.Generos
                        .Where(g => libroDto.IdGeneros.Contains(g.IdGenero))
                        .ToListAsync();

                    if (generos.Count != libroDto.IdGeneros.Count)
                        return BadRequest(new ApiResponse { Success = false, Message = "Uno o más géneros no existen" });
                }

                if (libroDto.IdEditorial == null)
                    {
                    libroDto.IdEditorial = editorialPorDefecto.IdEditorial;
                }

                

                // ✅ 8. CREAR EL LIBRO (TODAS LAS VALIDACIONES PASARON)
                var libro = new Libro
                {
                    Titulo = libroDto.Titulo.Trim(),
                    Isbn = libroDto.Isbn.Trim(),

                    IdEditorial = (int)libroDto.IdEditorial,
                    IdSeccion = (int)libroDto.IdSeccion,
                    Estado = libroDto.Estado ?? "disponible",
                    Descripcion = libroDto.Descripcion?.Trim() ?? "",
                    PortadaUrl = libroDto.PortadaUrl?.Trim() ?? "/imagenes/portadas/default-book-cover.jpg",
                    IdAutors = autores,
                    IdGeneros = generos ?? new List<Genero>()
                };

                // ✅ 9. GUARDAR EN BD
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
                    Message = $"Error al crear el libro: {ex.InnerException.Message}"
                });
            }
        }


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



    }
}
