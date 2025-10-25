using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
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
    }
}
