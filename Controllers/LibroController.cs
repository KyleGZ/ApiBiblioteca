using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                        Genero = l.IdAutors.Select(a => a.Nombre).ToList(),
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





    }
}
