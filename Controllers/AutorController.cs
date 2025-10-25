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
    }
}
