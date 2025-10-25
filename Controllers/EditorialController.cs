using ApiBiblioteca.Models;
using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
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
    }


}
