using ApiBiblioteca.Models;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificacionController : Controller
    {
        private readonly INotificacionesServices _notificacionesService;
        private readonly DbContextBiblioteca _context;

        public NotificacionController(INotificacionesServices notificaciones, DbContextBiblioteca dbContext)
        {
            _context = dbContext;
            _notificacionesService = notificaciones;
        }

        [HttpGet("ProbarRecordatorio")]
        public async Task<IActionResult> ProbarRecordatorio()
        {
            var resultado = await _notificacionesService.RecordarPrestamosPorVencerAsync();
            return Ok(resultado);
        }

    }
}
