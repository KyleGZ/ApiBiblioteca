using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
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
    }
}
