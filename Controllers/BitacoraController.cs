using ApiBiblioteca.Models.Dtos;
using ApiBiblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiBiblioteca.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BitacoraController : Controller
    {

        private readonly IBitacoraService _bitacoraService;


        public BitacoraController(IBitacoraService bitacora)
        {
            _bitacoraService = bitacora;
        }

        [HttpPost("RegistrarAccion")]
        public async Task<IActionResult> RegistrarAccion([FromBody] BitacoraRequest request)
        {
            try
            {
                // Validación básica del modelo
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState.Values.SelectMany(v => v.Errors) });

                // Llamar al servicio - NO esperamos que termine
                _ = _bitacoraService.RegistrarAccionAsync(
                    request.IdUsuario,
                    request.Accion,
                    request.TablaAfectada,
                    request.IdRegistro
                );

                // Respondemos inmediatamente sin esperar
                return Ok(new
                {
                    success = true,
                    message = "Solicitud de registro en bitácora aceptada"
                });
            }
            catch (Exception ex)
            {
                // Solo capturamos errores de validación, no del proceso en segundo plano
                Console.WriteLine($"Error en endpoint RegistrarAccion: {ex.Message}");
                return BadRequest(new { success = false, message = "Error en la solicitud" });
            }
        }


    }
}
